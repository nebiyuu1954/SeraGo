using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SeraGo.API.Services;

/// <summary>
/// Telegram bot service — handles account linking and message delivery.
/// The bot is created via @BotFather and configured via TELEGRAM_BOT_TOKEN env var.
///
/// Account linking flow:
/// 1. User opens Telegram, searches for @SeraGoBot, sends /start
/// 2. Bot replies with: "Link your SeraGo account: https://serago.et/telegram/link?token={token}"
/// 3. User clicks link → frontend calls POST /api/telegram/link with the token
/// 4. .NET stores the chat_id in UserSettings JSON blob
/// </summary>
public sealed class TelegramBotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly ITelegramBotClient? _botClient;
    private string? _botUsername; // cached from getMe

    public TelegramBotService(
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramBotService> logger,
        ITelegramBotClient? botClient = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _botClient = botClient;
    }

    /// <summary>
    /// Generates a short, signed linking token for a user.
    ///
    /// Layout: 16-byte user id + 4-byte expiry + 16-byte HMAC = 36 bytes, which
    /// base64url-encodes to 48 chars — short enough for Telegram's 64-char
    /// `?start=` deep-link payload, and unforgeable (the user id is covered by
    /// the MAC), unlike the previous raw-base64 encoding of the user id.
    /// </summary>
    public string GenerateLinkingToken(string userId)
    {
        Span<byte> buffer = stackalloc byte[LinkTokenBytes];
        Guid.Parse(userId).TryWriteBytes(buffer);

        var expiry = (uint)DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
        BinaryPrimitives.WriteUInt32BigEndian(buffer[16..20], expiry);

        var mac = ComputeLinkMac(buffer[..20]);
        mac.AsSpan(0, 16).CopyTo(buffer[20..]);

        return Base64UrlEncode(buffer);
    }

    /// <summary>
    /// Validates a linking token and returns the user ID if valid; null when
    /// malformed, tampered with, or expired.
    /// </summary>
    public string? ValidateLinkingToken(string token)
    {
        try
        {
            var buffer = Base64UrlDecode(token);
            if (buffer.Length != LinkTokenBytes) return null;

            var expected = ComputeLinkMac(buffer.AsSpan(0, 20));
            if (!CryptographicOperations.FixedTimeEquals(
                    buffer.AsSpan(20, 16), expected.AsSpan(0, 16)))
            {
                return null;
            }

            var expiry = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(16, 4));
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
                return null; // expired

            return new Guid(buffer.AsSpan(0, 16)).ToString();
        }
        catch
        {
            return null;
        }
    }

    // ── Linking-token crypto helpers ──

    private const int LinkTokenBytes = 36; // 16 id + 4 exp + 16 MAC

    /// <summary>
    /// HMAC-SHA256 over the token body. Keyed by TELEGRAM_LINK_SECRET, falling
    /// back to other always-present secrets so the default deployment works.
    /// </summary>
    private static byte[] ComputeLinkMac(ReadOnlySpan<byte> data)
    {
        var secret = Environment.GetEnvironmentVariable("TELEGRAM_LINK_SECRET")
            ?? Environment.GetEnvironmentVariable("INTERNAL_API_KEY")
            ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
            ?? "serago-dev-link-secret";

        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), data);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

    private static byte[] Base64UrlDecode(string token)
    {
        var padded = token.Replace("-", "+").Replace("_", "/");
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded,
        };
        return Convert.FromBase64String(padded);
    }

    /// <summary>
    /// Sends a direct alert to the system admin's Telegram chat.
    /// Uses TELEGRAM_CHAT_ID from the environment variables.
    /// </summary>
    public async Task SendAdminAlertAsync(string message)
    {
        if (_botClient is null) return;
        var adminChatIdEnv = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID");
        if (string.IsNullOrWhiteSpace(adminChatIdEnv) || !long.TryParse(adminChatIdEnv, out var chatId))
        {
            _logger.LogWarning("Cannot send admin alert: TELEGRAM_CHAT_ID is missing or invalid in environment.");
            return;
        }

        try
        {
            await _botClient.SendMessage(chatId, message, parseMode: ParseMode.Html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Telegram admin alert.");
        }
    }

    /// <summary>
    /// Registers the bot's webhook with Telegram servers.
    /// </summary>
    public async Task<bool> RegisterWebhookAsync(string webhookUrl)
    {
        if (_botClient is null) return false;
        try
        {
            await _botClient.SetWebhook(webhookUrl);
            _logger.LogInformation("Telegram webhook registered to {Url}", webhookUrl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register Telegram webhook to {Url}", webhookUrl);
            return false;
        }
    }

    /// <summary>
    /// The bot's @username (cached), used to build the t.me deep link that
    /// starts the account-linking flow. Null when the bot client is absent or
    /// Telegram can't be reached.
    /// </summary>
    public async Task<string?> GetBotUsernameAsync()
    {
        if (_botUsername is not null) return _botUsername;
        if (_botClient is null) return null;

        try
        {
            var me = await _botClient.GetMe();
            _botUsername = me.Username;
            return _botUsername;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch the Telegram bot username.");
            return null;
        }
    }

    /// <summary>
    /// Unlinks whichever account is bound to a Telegram chat id — used by the
    /// bot's /unlink command, where only the chat id is known.
    /// </summary>
    public async Task<bool> UnlinkByChatIdAsync(long chatId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // The chat id is stored in the UserSettings JSON blob; a LIKE match is
        // fine here (a rare, user-initiated command).
        var needle = $"\"chatId\":{chatId}";
        var userId = await db.UserSettings
            .Where(s => s.Settings.Contains(needle))
            .Select(s => s.UserId)
            .FirstOrDefaultAsync();

        return userId is not null && await UnlinkAccountAsync(userId);
    }

    /// <summary>
    /// Links a Telegram chat_id to a user's SeraGo account.
    /// Stores the chat_id in the UserSettings JSON blob.
    /// </summary>
    public async Task<bool> LinkAccountAsync(string userId, long chatId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings is null) return false;

        // Parse existing settings
        var settingsObj = string.IsNullOrWhiteSpace(settings.Settings)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(settings.Settings) ?? new();

        // Add/update telegram section
        settingsObj["telegram"] = new Dictionary<string, object>
        {
            ["linked"] = true,
            ["chatId"] = chatId,
            ["linkedAt"] = DateTimeOffset.UtcNow.ToString("O")
        };

        settings.Settings = JsonSerializer.Serialize(settingsObj);
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Telegram linked for user {UserId} chatId={ChatId}", userId, chatId);
        return true;
    }

    /// <summary>
    /// Unlinks a Telegram account from SeraGo.
    /// </summary>
    public async Task<bool> UnlinkAccountAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings is null) return false;

        var settingsObj = string.IsNullOrWhiteSpace(settings.Settings)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(settings.Settings) ?? new();

        if (settingsObj.ContainsKey("telegram"))
        {
            settingsObj["telegram"] = new Dictionary<string, object>
            {
                ["linked"] = false
            };

            settings.Settings = JsonSerializer.Serialize(settingsObj);
            settings.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation("Telegram unlinked for user {UserId}", userId);
        }

        return true;
    }

    /// <summary>
    /// Handles incoming messages and callback queries from Telegram users.
    /// Called by the webhook endpoint when the bot receives an update.
    /// </summary>
    public async Task HandleMessageAsync(Update update)
    {
        if (update.CallbackQuery is not null)
        {
            await HandleCallbackQueryAsync(update.CallbackQuery);
            return;
        }

        if (update.Message is not { } message)
            return;

        var chatId = message.Chat.Id;

        // User sent a document (e.g. CV file upload in Telegram)
        if (message.Document is not null)
        {
            await HandleDocumentUploadAsync(chatId);
            return;
        }

        var text = message.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        _logger.LogInformation("Telegram message from {ChatId}: {Text}", chatId, text);

        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                var token = parts[1];
                var linkedUserId = ValidateLinkingToken(token);
                if (linkedUserId is not null)
                {
                    var linked = await LinkAccountAsync(linkedUserId, chatId);
                    if (linked)
                    {
                        await SendTalentDashboardCardAsync(chatId, linkedUserId, "🎉 <b>Your SeraGo account has been successfully linked!</b>");
                        return;
                    }
                }
            }

            var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
            if (linkedUser is not null)
            {
                await SendTalentDashboardCardAsync(chatId, linkedUser.Value.UserId, null);
                return;
            }

            await SendUnlinkedWelcomeCardAsync(chatId);
        }
        else if (text.StartsWith("/sectors", StringComparison.OrdinalIgnoreCase) ||
                 text.StartsWith("/mysectors", StringComparison.OrdinalIgnoreCase))
        {
            var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
            if (linkedUser is not null)
            {
                await SendTalentDashboardCardAsync(chatId, linkedUser.Value.UserId, null);
            }
            else
            {
                await SendUnlinkedWelcomeCardAsync(chatId);
            }
        }
        else if (text.StartsWith("/cv", StringComparison.OrdinalIgnoreCase) ||
                 text.StartsWith("/resume", StringComparison.OrdinalIgnoreCase))
        {
            var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
            if (linkedUser is not null)
            {
                await SendCvStatusCardAsync(chatId, linkedUser.Value.UserId);
            }
            else
            {
                await SendUnlinkedWelcomeCardAsync(chatId);
            }
        }
        else if (text.StartsWith("/unlink", StringComparison.OrdinalIgnoreCase))
        {
            var unlinked = await UnlinkByChatIdAsync(chatId);
            await _botClient!.SendMessage(chatId,
                unlinked
                    ? "🔌 Your SeraGo account has been unlinked. You won't receive job alerts here."
                    : "This Telegram chat isn't linked to a SeraGo account.",
                parseMode: ParseMode.Html);
        }
        else if (text.StartsWith("/settings", StringComparison.OrdinalIgnoreCase))
        {
            await _botClient!.SendMessage(chatId,
                "⚙️ <b>Manage notifications in SeraGo:</b>\n" +
                "<a href=\"https://serago.pro.et/dashboard/settings#telegram\">Open Settings → Notifications</a>\n\n" +
                "Keep the <b>Telegram</b> toggle on for job alerts to receive real-time notifications.",
                parseMode: ParseMode.Html);
        }
        else if (text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            var help =
                "<b>Available Commands:</b>\n\n" +
                "/start - Open bot dashboard & CV/sector alerts\n" +
                "/sectors - View and manage your alert sectors\n" +
                "/cv - Check your CV upload status & AI matching\n" +
                "/settings - Notification preferences on SeraGo\n" +
                "/unlink - Disconnect this Telegram chat\n" +
                "/help - Show this guide";

            await _botClient!.SendMessage(chatId, help, parseMode: ParseMode.Html);
        }
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        if (_botClient is null) return;
        var chatId = callbackQuery.Message?.Chat.Id;
        var data = callbackQuery.Data;
        if (chatId is null || string.IsNullOrWhiteSpace(data)) return;

        if (data.StartsWith("toggle_sector:"))
        {
            var sectorIdStr = data["toggle_sector:".Length..];
            if (Guid.TryParse(sectorIdStr, out var sectorId))
            {
                var result = await ToggleUserSectorAsync(chatId.Value, sectorId);
                if (result is not null)
                {
                    var (toggledOn, sectorName, updatedMarkup) = result.Value;
                    var toast = toggledOn ? $"✅ Subscribed to {sectorName}!" : $"❌ Removed {sectorName}";
                    try
                    {
                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: toast);
                        await _botClient.EditMessageReplyMarkup(
                            chatId: chatId.Value,
                            messageId: callbackQuery.Message!.MessageId,
                            replyMarkup: updatedMarkup);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update Telegram callback query reply markup");
                    }
                }
            }
        }
        else if (data == "refresh_sectors")
        {
            var linkedUser = await FindLinkedUserByChatIdAsync(chatId.Value);
            if (linkedUser is not null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id);
                await SendTalentDashboardCardAsync(chatId.Value, linkedUser.Value.UserId, null);
            }
        }
    }

    private async Task HandleDocumentUploadAsync(long chatId)
    {
        if (_botClient is null) return;
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("📄 Upload CV to SeraGo Profile", "https://serago.pro.et/dashboard/talent/profile") },
            new[] { InlineKeyboardButton.WithCallbackData("🎯 Pick Target Sectors", "refresh_sectors") }
        });

        await _botClient.SendMessage(
            chatId,
            "📄 <b>Document Received!</b>\n\n" +
            "To connect your CV for AI job compatibility scoring and 1-click applications, please upload it directly to your profile on SeraGo:\n\n" +
            "👉 <a href=\"https://serago.pro.et/dashboard/talent/profile\">Upload CV on SeraGo Profile</a>\n\n" +
            "You can also select your preferred job sectors below right now to start receiving job alerts!",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard);
    }

    private async Task SendTalentDashboardCardAsync(long chatId, string userId, string? headerNotice)
    {
        if (_botClient is null) return;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var profile = await db.TalentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        var settings = await db.UserSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);

        var hasCv = !string.IsNullOrWhiteSpace(profile?.ResumeUrl);
        var selectedSectorIds = UserSettingsReader.GetForYouSectorIds(settings?.Settings);
        var allSectors = await db.Sectors.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).Take(10).ToListAsync();

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(headerNotice))
        {
            sb.AppendLine(headerNotice);
            sb.AppendLine();
        }

        var firstName = !string.IsNullOrWhiteSpace(user?.FirstName) ? user.FirstName : "there";
        sb.AppendLine($"👋 <b>Welcome to SeraGo Bot, {Escape(firstName)}!</b>");
        sb.AppendLine();

        if (hasCv)
        {
            sb.AppendLine("📄 <b>CV Status:</b> ✅ Uploaded on SeraGo");
            sb.AppendLine("Your resume is active for AI job compatibility scoring.");
        }
        else
        {
            sb.AppendLine("📄 <b>CV Status:</b> ⚠️ No CV on file");
            sb.AppendLine("Upload your CV on SeraGo for AI matching, or pick your job sectors below to receive tailored alerts right here.");
        }

        sb.AppendLine();
        sb.AppendLine("🎯 <b>Your Job Alert Sectors:</b>");
        if (selectedSectorIds.Count > 0)
        {
            var sectorNames = allSectors.Where(s => selectedSectorIds.Contains(s.Id)).Select(s => s.Name).ToList();
            var displayNames = sectorNames.Count > 0 ? string.Join(", ", sectorNames) : $"{selectedSectorIds.Count} selected";
            sb.AppendLine($"Subscribed: <i>{Escape(displayNames)}</i>");
        }
        else
        {
            sb.AppendLine("<i>No sectors selected yet. Tap buttons below to subscribe:</i>");
        }

        sb.AppendLine();
        sb.AppendLine("👇 <i>Tap any sector below to toggle job alerts on/off:</i>");

        var keyboard = BuildSectorPickerKeyboard(allSectors, selectedSectorIds.ToHashSet(), hasCv);

        await _botClient.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
    }

    private async Task SendCvStatusCardAsync(long chatId, string userId)
    {
        if (_botClient is null) return;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var profile = await db.TalentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        var hasCv = !string.IsNullOrWhiteSpace(profile?.ResumeUrl);

        if (hasCv)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl("🌐 View Profile on SeraGo", "https://serago.pro.et/dashboard/talent/profile") }
            });

            await _botClient.SendMessage(
                chatId,
                "✅ <b>Your CV is connected!</b>\n\n" +
                "Your resume is active. SeraGo's AI automatically matches incoming jobs with your skills and work experience, delivering personalized alerts right here.\n\n" +
                "Use /sectors to check or customize the job sectors you want alerts for.",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
        }
        else
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl("📄 Upload CV on SeraGo", "https://serago.pro.et/dashboard/talent/profile") },
                new[] { InlineKeyboardButton.WithCallbackData("🎯 Pick Alert Sectors Now", "refresh_sectors") }
            });

            await _botClient.SendMessage(
                chatId,
                "📄 <b>No CV on File</b>\n\n" +
                "You haven't uploaded a CV yet. When you upload your CV on SeraGo, our AI:\n" +
                "• Calculates your compatibility score for new jobs\n" +
                "• Highlights matching skills\n" +
                "• Sends high-confidence job alerts directly to this chat\n\n" +
                "Upload your CV on SeraGo, or pick your sectors now to receive sector-matched job alerts!",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
        }
    }

    private async Task SendUnlinkedWelcomeCardAsync(long chatId)
    {
        if (_botClient is null) return;
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🔗 Connect SeraGo Account", "https://serago.pro.et/dashboard/settings#telegram") },
            new[] { InlineKeyboardButton.WithUrl("🚀 Create Free Account", "https://serago.pro.et/signup") },
            new[] { InlineKeyboardButton.WithUrl("📢 Join SeraGo Job Channel", "https://t.me/SeraGo2026") },
            new[] { InlineKeyboardButton.WithUrl("🔍 Browse Latest Jobs", "https://serago.pro.et/dashboard/talent/jobs") }
        });

        var message =
            "👋 <b>Welcome to SeraGo Bot!</b>\n\n" +
            "I am your personal AI Job Alert Assistant for opportunities across Ethiopia.\n\n" +
            "<b>How to connect your account:</b>\n" +
            "1. Tap <b>🔗 Connect SeraGo Account</b> below\n" +
            "2. In Settings, the <b>Notifications</b> panel will open\n" +
            "3. Tap the blue <b>[ Connect Telegram ]</b> button\n" +
            "4. Your browser will bounce back to this chat and link your account instantly! 🎉\n\n" +
            "<i>Don't have an account yet? Tap 'Create Free Account' to get started in 30 seconds.</i>";

        await _botClient.SendMessage(chatId, message, parseMode: ParseMode.Html, replyMarkup: keyboard);
    }

    private async Task<(string UserId, string? FirstName)?> FindLinkedUserByChatIdAsync(long chatId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var needle = $"\"chatId\":{chatId}";
        var setting = await db.UserSettings.AsNoTracking()
            .Where(s => s.Settings.Contains(needle))
            .Select(s => new { s.UserId })
            .FirstOrDefaultAsync();

        if (setting is null) return null;

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == setting.UserId)
            .Select(u => new { u.Id, u.FirstName })
            .FirstOrDefaultAsync();

        return user is not null ? (user.Id, user.FirstName) : null;
    }

    private async Task<(bool ToggledOn, string SectorName, InlineKeyboardMarkup UpdatedMarkup)?> ToggleUserSectorAsync(
        long chatId,
        Guid sectorId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var needle = $"\"chatId\":{chatId}";
        var settingsEntity = await db.UserSettings.FirstOrDefaultAsync(s => s.Settings.Contains(needle));
        if (settingsEntity is null) return null;

        var sector = await db.Sectors.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sectorId && s.IsActive);
        if (sector is null) return null;

        var currentIds = UserSettingsReader.GetForYouSectorIds(settingsEntity.Settings);
        bool toggledOn;
        if (currentIds.Contains(sectorId))
        {
            currentIds.Remove(sectorId);
            toggledOn = false;
        }
        else
        {
            currentIds.Add(sectorId);
            toggledOn = true;
        }

        settingsEntity.Settings = UpdateForYouSectorIdsInJson(settingsEntity.Settings, currentIds);
        settingsEntity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var profile = await db.TalentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == settingsEntity.UserId);
        var allSectors = await db.Sectors.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).Take(10).ToListAsync();
        var hasCv = !string.IsNullOrWhiteSpace(profile?.ResumeUrl);

        var markup = BuildSectorPickerKeyboard(allSectors, currentIds.ToHashSet(), hasCv);
        return (toggledOn, sector.Name, markup);
    }

    private static InlineKeyboardMarkup BuildSectorPickerKeyboard(
        List<Sector> allSectors,
        HashSet<Guid> selectedSectorIds,
        bool hasCv)
    {
        var rows = new List<InlineKeyboardButton[]>();

        if (!hasCv)
        {
            rows.Add(new[]
            {
                InlineKeyboardButton.WithUrl("📄 Upload CV on SeraGo (AI Matching)", "https://serago.pro.et/dashboard/talent/profile")
            });
        }

        foreach (var s in allSectors)
        {
            var isSel = selectedSectorIds.Contains(s.Id);
            var label = isSel ? $"✅ {s.Name}" : $"➕ {s.Name}";
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(label, $"toggle_sector:{s.Id}") });
        }

        rows.Add(new[]
        {
            InlineKeyboardButton.WithUrl("🌐 Open Profile on SeraGo", "https://serago.pro.et/dashboard/talent/profile")
        });

        return new InlineKeyboardMarkup(rows);
    }

    private static string UpdateForYouSectorIdsInJson(string? settingsJson, List<Guid> sectorIds)
    {
        JsonNode root;
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            root = new JsonObject();
        }
        else
        {
            try { root = JsonNode.Parse(settingsJson) ?? new JsonObject(); }
            catch { root = new JsonObject(); }
        }

        var forYou = root["forYou"] as JsonObject;
        if (forYou is null)
        {
            forYou = new JsonObject();
            root["forYou"] = forYou;
        }

        var arr = new JsonArray();
        foreach (var id in sectorIds)
        {
            arr.Add(id.ToString());
        }
        forYou["sectorIds"] = arr;
        forYou["onboardingCompleted"] = true;

        if (root["notifications"] is JsonObject notifs && notifs["jobAlerts"] is JsonObject jobAlerts)
        {
            jobAlerts["telegram"] = true;
        }

        return root.ToJsonString();
    }

    private static string Escape(string? text) => string.IsNullOrEmpty(text)
        ? string.Empty
        : text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
