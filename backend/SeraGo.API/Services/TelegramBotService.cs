using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
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
    private readonly ConcurrentDictionary<long, int> _pendingUploadPromptMessageIds = new();

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

    /// <summary>
    /// Generates a short, signed token for a Telegram chatId.
    /// Layout: 8-byte chatId (long) + 4-byte expiry + 16-byte HMAC = 28 bytes (38 chars base64url).
    /// </summary>
    public string GenerateChatLinkingToken(long chatId)
    {
        Span<byte> buffer = stackalloc byte[ChatLinkTokenBytes];
        BinaryPrimitives.WriteInt64BigEndian(buffer[..8], chatId);

        var expiry = (uint)DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
        BinaryPrimitives.WriteUInt32BigEndian(buffer[8..12], expiry);

        var mac = ComputeLinkMac(buffer[..12]);
        mac.AsSpan(0, 16).CopyTo(buffer[12..]);

        return Base64UrlEncode(buffer);
    }

    /// <summary>
    /// Validates a chat linking token and returns the chatId if valid; null if invalid or expired.
    /// </summary>
    public long? ValidateChatLinkingToken(string token)
    {
        try
        {
            var buffer = Base64UrlDecode(token);
            if (buffer.Length != ChatLinkTokenBytes) return null;

            var expected = ComputeLinkMac(buffer.AsSpan(0, 12));
            if (!CryptographicOperations.FixedTimeEquals(
                    buffer.AsSpan(12, 16), expected.AsSpan(0, 16)))
            {
                return null;
            }

            var expiry = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(8, 4));
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
                return null; // expired

            return BinaryPrimitives.ReadInt64BigEndian(buffer.AsSpan(0, 8));
        }
        catch
        {
            return null;
        }
    }

    // ── Linking-token crypto helpers ──

    private const int LinkTokenBytes = 36; // 16 id + 4 exp + 16 MAC
    private const int ChatLinkTokenBytes = 28; // 8 chatId + 4 exp + 16 MAC

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
        var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
        return linkedUser is not null && await UnlinkAccountAsync(linkedUser.Value.UserId);
    }

    /// <summary>
    /// Returns whether the user has a Telegram bot linked and the associated chat_id.
    /// </summary>
    public async Task<(bool Linked, long? ChatId)> GetUserTelegramStatusAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var settingsJson = await db.UserSettings.AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.Settings)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(settingsJson))
            return (false, null);

        try
        {
            var doc = JsonDocument.Parse(settingsJson);
            if (doc.RootElement.TryGetProperty("telegram", out var tg))
            {
                var isLinked = tg.TryGetProperty("linked", out var linkedProp) && linkedProp.GetBoolean();
                long? chatId = null;
                if (tg.TryGetProperty("chatId", out var chatProp))
                {
                    if (chatProp.ValueKind == JsonValueKind.Number && chatProp.TryGetInt64(out var idNum))
                        chatId = idNum;
                    else if (chatProp.ValueKind == JsonValueKind.String && long.TryParse(chatProp.GetString(), out var idParsed))
                        chatId = idParsed;
                }
                return (isLinked, chatId);
            }
        }
        catch { }

        return (false, null);
    }

    /// <summary>
    /// Fetches the Telegram chat info (username, first name) for a given chat id.
    /// </summary>
    public async Task<(string? Username, string? FirstName)?> GetChatInfoAsync(long chatId)
    {
        if (_botClient is null) return null;
        try
        {
            var chat = await _botClient.GetChat(chatId);
            return (chat.Username, chat.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch Telegram chat details for chatId {ChatId}", chatId);
            return null;
        }
    }

    /// <summary>
    /// Links a Telegram chat_id to a user's SeraGo account.
    /// Stores the chat_id in the UserSettings JSON blob.
    /// </summary>
    public async Task<bool> LinkAccountAsync(string userId, long chatId, string? username = null, string? firstName = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings is null) return false;

        // If username/firstName wasn't provided, try resolving from Telegram
        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(firstName) && _botClient is not null)
        {
            try
            {
                var chat = await _botClient.GetChat(chatId);
                username = chat.Username;
                firstName = chat.FirstName;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not fetch Telegram chat details for chatId {ChatId}", chatId);
            }
        }

        JsonNode root;
        if (string.IsNullOrWhiteSpace(settings.Settings))
        {
            root = new JsonObject();
        }
        else
        {
            try { root = JsonNode.Parse(settings.Settings) ?? new JsonObject(); }
            catch { root = new JsonObject(); }
        }

        // Add/update telegram section
        var telegramNode = new JsonObject
        {
            ["linked"] = true,
            ["chatId"] = chatId,
            ["linkedAt"] = DateTimeOffset.UtcNow.ToString("O")
        };
        if (!string.IsNullOrWhiteSpace(username))
        {
            telegramNode["username"] = username;
        }
        if (!string.IsNullOrWhiteSpace(firstName))
        {
            telegramNode["firstName"] = firstName;
        }
        root["telegram"] = telegramNode;

        // Ensure notifications section has telegram enabled for jobAlerts and applicationUpdates
        var notifs = root["notifications"] as JsonObject;
        if (notifs is null)
        {
            notifs = new JsonObject();
            root["notifications"] = notifs;
        }

        EnsureChannelToggle(notifs, "jobAlerts", telegram: true);
        EnsureChannelToggle(notifs, "applicationUpdates", telegram: true);

        settings.Settings = root.ToJsonString();
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Telegram linked for user {UserId} chatId={ChatId}", userId, chatId);
        return true;
    }

    private static void EnsureChannelToggle(JsonObject notifs, string key, bool telegram)
    {
        if (notifs[key] is JsonObject toggle)
        {
            toggle["telegram"] = telegram;
            if (!toggle.ContainsKey("inApp")) toggle["inApp"] = true;
            if (!toggle.ContainsKey("email")) toggle["email"] = true;
        }
        else
        {
            notifs[key] = new JsonObject
            {
                ["email"] = true,
                ["inApp"] = true,
                ["telegram"] = telegram
            };
        }
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
            await HandleDocumentUploadAsync(chatId, message.Document);
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
                    // Conflict check 1: Chat is already linked to another SeraGo account
                    var existingChatUser = await FindLinkedUserByChatIdAsync(chatId);
                    if (existingChatUser is not null && existingChatUser.Value.UserId != linkedUserId)
                    {
                        await _botClient.SendMessage(chatId,
                            "⚠️ <b>Account Conflict:</b> This Telegram chat is already connected to another SeraGo account. Please unlink that account first using /unlink.",
                            parseMode: ParseMode.Html);
                        return;
                    }

                    // Conflict check 2: SeraGo account is already linked to another Telegram bot
                    var (isUserLinked, existingUserChatId) = await GetUserTelegramStatusAsync(linkedUserId);
                    if (isUserLinked && existingUserChatId != chatId)
                    {
                        await _botClient.SendMessage(chatId,
                            "⚠️ <b>Account Conflict:</b> Your SeraGo account already has a Telegram bot connected. Only one Telegram bot is allowed per account. Disconnect it in your SeraGo Settings before connecting this one.",
                            parseMode: ParseMode.Html);
                        return;
                    }

                    var linked = await LinkAccountAsync(linkedUserId, chatId, message.From?.Username, message.From?.FirstName);
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
        else if (text.StartsWith("/testalert", StringComparison.OrdinalIgnoreCase) ||
                 text.StartsWith("/latest", StringComparison.OrdinalIgnoreCase))
        {
            await SendTestAlertAsync(chatId);
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
                await SendOrEditCvManagementCardAsync(chatId, null, linkedUser.Value.UserId);
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
                "/testalert - Send latest matching job alert right now\n" +
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
        var chatId = callbackQuery.Message?.Chat.Id ?? callbackQuery.From.Id;
        var data = callbackQuery.Data;
        if (string.IsNullOrWhiteSpace(data))
        {
            try { await _botClient.AnswerCallbackQuery(callbackQuery.Id); } catch { }
            return;
        }

        _logger.LogInformation("Telegram CallbackQuery received from chat {ChatId}: {Data}", chatId, data);

        if (data == "send_test_alert")
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: "Sending latest matching job alert...");
            await SendTestAlertAsync(chatId);
            return;
        }

        if (data == "stop_telegram_notifs")
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: "🔕 Sector notifications stopped");
            await SetTelegramNotificationsEnabledAsync(chatId, enabled: false);
            var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
            if (linkedUser is not null)
            {
                await EditOrSendTalentDashboardCardAsync(chatId, callbackQuery.Message?.MessageId, linkedUser.Value.UserId, "🔕 <b>Sector notifications have been paused.</b>");
            }
            return;
        }

        if (data == "start_telegram_notifs")
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: "🔔 Sector notifications turned on!");
            await SetTelegramNotificationsEnabledAsync(chatId, enabled: true);
            var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
            if (linkedUser is not null)
            {
                await EditOrSendTalentDashboardCardAsync(chatId, callbackQuery.Message?.MessageId, linkedUser.Value.UserId, "🔔 <b>Sector notifications are now active!</b>");
            }
            return;
        }

        if (data == "show_dashboard")
        {
            _pendingUploadPromptMessageIds.TryRemove(chatId, out _);
            await _botClient.AnswerCallbackQuery(callbackQuery.Id);
            var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
            if (linkedUser is not null)
            {
                await EditOrSendTalentDashboardCardAsync(chatId, callbackQuery.Message?.MessageId, linkedUser.Value.UserId, null);
            }
            else
            {
                await SendUnlinkedWelcomeCardAsync(chatId);
            }
            return;
        }

        if (data == "manage_cv")
        {
            var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
            if (linkedUser is not null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id);
                await SendOrEditCvManagementCardAsync(chatId, callbackQuery.Message?.MessageId, linkedUser.Value.UserId);
            }
            else
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: "⚠️ Please connect your SeraGo account first.", showAlert: true);
                await SendUnlinkedWelcomeCardAsync(chatId);
            }
            return;
        }

        if (data == "download_cv")
        {
            var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
            if (linkedUser is not null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: "📥 Fetching your CV...");
                await DownloadAndSendCvAsync(chatId, linkedUser.Value.UserId, callbackQuery.Message?.MessageId);
            }
            else
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: "⚠️ Please connect your SeraGo account first.", showAlert: true);
                await SendUnlinkedWelcomeCardAsync(chatId);
            }
            return;
        }

        if (data == "prompt_upload_cv")
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id);
            await SendCvUploadPromptAsync(chatId, callbackQuery.Message?.MessageId);
            return;
        }

        if (data == "open_sector_picker" || data == "refresh_sectors")
        {
            var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
            if (linkedUser is not null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id);
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var settings = await db.UserSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == linkedUser.Value.UserId);
                var selectedSectorIds = UserSettingsReader.GetForYouSectorIds(settings?.Settings);
                var allSectors = await db.Sectors.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                var keyboard = BuildSectorPickerKeyboard(allSectors, selectedSectorIds.ToHashSet());

                var pickerText = "🎯 <b>Select your job sector:</b>\n\nTap any sector below to switch your job alerts to that sector:";
                try
                {
                    if (callbackQuery.Message is not null)
                    {
                        await _botClient.EditMessageText(
                            chatId: chatId,
                            messageId: callbackQuery.Message.MessageId,
                            text: pickerText,
                            parseMode: ParseMode.Html,
                            replyMarkup: keyboard);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to edit message for sector picker, falling back to SendMessage");
                }

                await _botClient.SendMessage(chatId, pickerText, parseMode: ParseMode.Html, replyMarkup: keyboard);
            }
            else
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: "⚠️ Please connect your SeraGo account first.", showAlert: true);
                await SendUnlinkedWelcomeCardAsync(chatId);
            }
            return;
        }

        if (data.StartsWith("select_sector:") || data.StartsWith("toggle_sector:"))
        {
            var sectorIdStr = data.Contains(':') ? data[(data.IndexOf(':') + 1)..] : "";
            if (Guid.TryParse(sectorIdStr, out var sectorId))
            {
                var newSectorName = await SetUserSectorAsync(chatId, sectorId);
                if (newSectorName is not null)
                {
                    await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: $"✅ Sector changed to {newSectorName}!");
                    var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
                    if (linkedUser is not null)
                    {
                        var notice = $"✅ <b>Sector changed to {Escape(newSectorName)}!</b>";
                        await EditOrSendTalentDashboardCardAsync(chatId, callbackQuery.Message?.MessageId, linkedUser.Value.UserId, notice);
                    }
                }
                else
                {
                    await _botClient.AnswerCallbackQuery(callbackQuery.Id, text: "⚠️ Sector could not be updated.");
                }
            }
            else
            {
                try { await _botClient.AnswerCallbackQuery(callbackQuery.Id); } catch { }
            }
            return;
        }

        // Always answer callback query if unhandled so client button spinner stops
        try { await _botClient.AnswerCallbackQuery(callbackQuery.Id); } catch { }
    }

    private async Task HandleDocumentUploadAsync(long chatId, Document document)
    {
        if (_botClient is null) return;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetService<R2StorageService>();

        var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
        if (linkedUser is null)
        {
            await _botClient.SendMessage(
                chatId,
                "⚠️ <b>Account Not Connected!</b>\n\nPlease connect your SeraGo account before uploading your CV so we can attach it to your profile.",
                parseMode: ParseMode.Html);
            await SendUnlinkedWelcomeCardAsync(chatId);
            return;
        }

        // Validate file extension
        var fileName = document.FileName ?? "resume.pdf";
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExts = new[] { ".pdf", ".docx", ".doc" };
        if (!allowedExts.Contains(ext))
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("« Back to Manage CV", "manage_cv") }
            });

            await _botClient.SendMessage(
                chatId,
                "⚠️ <b>Unsupported File Format!</b>\n\n" +
                $"You uploaded: <code>{Escape(fileName)}</code>\n\n" +
                "Please upload your CV in one of the accepted formats:\n" +
                "• <b>PDF (.pdf)</b>\n" +
                "• <b>Word (.docx, .doc)</b>",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
            return;
        }

        // Validate file size: 10 MB maximum
        const long maxSizeBytes = 10 * 1024 * 1024;
        if (document.FileSize > maxSizeBytes)
        {
            var sizeMb = Math.Round((double)(document.FileSize ?? 0) / (1024 * 1024), 1);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("« Back to Manage CV", "manage_cv") }
            });

            await _botClient.SendMessage(
                chatId,
                $"⚠️ <b>File Too Large ({sizeMb} MB)!</b>\n\n" +
                "The maximum allowed CV file size is <b>10 MB</b>. Please compress or choose a smaller file.",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
            return;
        }

        if (storage is null || !storage.IsConfigured)
        {
            await _botClient.SendMessage(chatId, "⚠️ File storage is not configured on this server.", parseMode: ParseMode.Html);
            return;
        }

        var progressMsg = await _botClient.SendMessage(
            chatId,
            "⏳ <i>Uploading and updating your CV on SeraGo...</i>",
            parseMode: ParseMode.Html);

        try
        {
            // 1. Download file from Telegram
            var tgFile = await _botClient.GetFile(document.FileId);
            if (string.IsNullOrWhiteSpace(tgFile.FilePath))
            {
                throw new Exception("Telegram did not provide a FilePath.");
            }

            using var memoryStream = new MemoryStream();
            await _botClient.DownloadFile(tgFile.FilePath, memoryStream);
            memoryStream.Position = 0;

            // 2. Upload to Cloudflare R2
            var contentType = document.MimeType ?? (ext == ".pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            var newKey = await storage.UploadFileAsync(linkedUser.Value.UserId, fileName, contentType, "resumes", memoryStream);

            // 3. Update TalentProfile in database
            var profile = await db.TalentProfiles.FirstOrDefaultAsync(p => p.UserId == linkedUser.Value.UserId);
            if (profile is null)
            {
                profile = new TalentProfile
                {
                    UserId = linkedUser.Value.UserId,
                    ResumeUrl = newKey,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.TalentProfiles.Add(profile);
            }
            else
            {
                var oldKey = profile.ResumeUrl;
                profile.ResumeUrl = newKey;
                profile.UpdatedAt = DateTime.UtcNow;

                // 4. Delete old CV from storage if present and different
                if (!string.IsNullOrWhiteSpace(oldKey) && oldKey != newKey)
                {
                    try { await storage.DeleteFileAsync(oldKey); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete previous CV {OldKey}", oldKey); }
                }
            }

            await db.SaveChangesAsync();

            // 5. Trigger MatchingClient resume parsing in background if available
            _ = Task.Run(async () =>
            {
                try
                {
                    using var parseScope = _scopeFactory.CreateScope();
                    var matchingClient = parseScope.ServiceProvider.GetService<MatchingClient>();
                    var r2 = parseScope.ServiceProvider.GetService<R2StorageService>();
                    if (matchingClient is not null && r2 is not null)
                    {
                        var presignedUrl = r2.GetPresignedDownloadUrlAsync(newKey);
                        await matchingClient.ParseResumeAsync(presignedUrl);
                    }
                }
                catch { }
            });

            // 6. Delete progress message and prompt message
            try { await _botClient.DeleteMessage(chatId, progressMsg.MessageId); } catch { }
            if (_pendingUploadPromptMessageIds.TryRemove(chatId, out var uploadPromptId))
            {
                try { await _botClient.DeleteMessage(chatId, uploadPromptId); } catch { }
            }

            var fileSizeKb = Math.Round((double)(document.FileSize ?? 0) / 1024, 1);
            await SendTalentDashboardCardAsync(
                chatId,
                linkedUser.Value.UserId,
                $"🎉 <b>Successfully updated CV:</b> <code>{Escape(fileName)}</code> ({fileSizeKb} KB)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload CV document from Telegram for user {UserId}", linkedUser.Value.UserId);
            try { await _botClient.DeleteMessage(chatId, progressMsg.MessageId); } catch { }
            await _botClient.SendMessage(
                chatId,
                "⚠️ <b>Upload Failed!</b>\nWe could not save your CV file. Please try again or upload directly on your SeraGo profile.",
                parseMode: ParseMode.Html);
        }
    }

    public async Task<(string Text, InlineKeyboardMarkup Markup)> BuildTalentDashboardContentAsync(
        ApplicationDbContext db, string userId, string? headerNotice)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var profile = await db.TalentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        var settings = await db.UserSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);

        var hasCv = !string.IsNullOrWhiteSpace(profile?.ResumeUrl);
        var selectedSectorIds = UserSettingsReader.GetForYouSectorIds(settings?.Settings);
        List<string> sectorNames = new();
        if (selectedSectorIds.Count > 0)
        {
            sectorNames = await db.Sectors.AsNoTracking()
                .Where(s => selectedSectorIds.Contains(s.Id))
                .Select(s => s.Name)
                .ToListAsync();
        }

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
            sb.AppendLine("Upload your CV on SeraGo for AI compatibility scoring.");
        }

        sb.AppendLine();
        sb.AppendLine("🎯 <b>Your Job Alert Sectors:</b>");

        if (selectedSectorIds.Count > 0)
        {
            var displayNames = sectorNames.Count > 0 ? string.Join(", ", sectorNames) : $"{selectedSectorIds.Count} selected";
            sb.AppendLine($"Subscribed: <b>{Escape(displayNames)}</b>");
            sb.AppendLine();

            var isTelegramEnabled = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(settings?.Settings))
                {
                    var doc = JsonDocument.Parse(settings.Settings);
                    if (doc.RootElement.TryGetProperty("notifications", out var notifs) &&
                        notifs.TryGetProperty("jobAlerts", out var ja) &&
                        ja.TryGetProperty("telegram", out var tg))
                    {
                        isTelegramEnabled = tg.GetBoolean();
                    }
                }
            }
            catch { }

            if (isTelegramEnabled)
            {
                sb.AppendLine("⚡ <i>Instant job alerts are active! Whenever a new job matching your sector is posted, it will be delivered directly here.</i>");
            }
            else
            {
                sb.AppendLine("🔕 <i>Telegram job alerts are currently stopped.</i>");
            }

            var stopOrStartButton = isTelegramEnabled
                ? InlineKeyboardButton.WithCallbackData("🔕 Stop Sector Notifications", "stop_telegram_notifs")
                : InlineKeyboardButton.WithCallbackData("🔔 Turn On Sector Notifications", "start_telegram_notifs");

            var cvButton = InlineKeyboardButton.WithCallbackData(hasCv ? "📄 Manage CV" : "➕ Add CV", "manage_cv");

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✏️ Change Sector", "open_sector_picker") },
                new[] { stopOrStartButton },
                new[] { cvButton }
            });

            return (sb.ToString(), keyboard);
        }
        else
        {
            sb.AppendLine("<i>No alert sector selected yet.</i>");
            sb.AppendLine();
            sb.AppendLine("👇 <b>Select your job sector to activate alerts:</b>");

            var allSectors = await db.Sectors.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            var pickerKeyboard = BuildSectorPickerKeyboard(allSectors, selectedSectorIds.ToHashSet());
            return (sb.ToString(), pickerKeyboard);
        }
    }

    public async Task EditOrSendTalentDashboardCardAsync(
        long chatId,
        int? messageId,
        string userId,
        string? headerNotice)
    {
        if (_botClient is null) return;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var (text, keyboard) = await BuildTalentDashboardContentAsync(db, userId, headerNotice);

        if (messageId is not null)
        {
            try
            {
                await _botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: text,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not edit message {MessageId}, falling back to SendMessage", messageId);
            }
        }

        await _botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: keyboard);
    }

    public Task SendTalentDashboardCardAsync(long chatId, string userId, string? headerNotice) =>
        EditOrSendTalentDashboardCardAsync(chatId, null, userId, headerNotice);

    /// <summary>
    /// Sends the latest published job in the user's subscribed sector as an instant test alert.
    /// </summary>
    public async Task SendTestAlertAsync(long chatId)
    {
        if (_botClient is null) return;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
        if (linkedUser is null)
        {
            await _botClient.SendMessage(chatId, "⚠️ Please connect your SeraGo account first to receive job alerts.");
            return;
        }

        var settings = await db.UserSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == linkedUser.Value.UserId);
        var selectedSectorIds = UserSettingsReader.GetForYouSectorIds(settings?.Settings);

        Job? testJob = null;
        string? sectorName = null;

        if (selectedSectorIds.Count > 0)
        {
            testJob = await db.Jobs.AsNoTracking()
                .Include(j => j.Sector)
                .Where(j => j.Status == JobStatus.Published && j.IsActive && j.SectorId != null && selectedSectorIds.Contains(j.SectorId.Value))
                .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
                .FirstOrDefaultAsync();

            if (testJob?.Sector is not null) sectorName = testJob.Sector.Name;
            else if (testJob?.SectorName is not null) sectorName = testJob.SectorName;
            else
            {
                sectorName = await db.Sectors.AsNoTracking()
                    .Where(s => selectedSectorIds.Contains(s.Id))
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();
            }
        }

        if (testJob is null)
        {
            testJob = await db.Jobs.AsNoTracking()
                .Include(j => j.Sector)
                .Where(j => j.Status == JobStatus.Published && j.IsActive)
                .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (testJob is null)
        {
            await _botClient.SendMessage(chatId, "ℹ️ No published jobs found on the platform to test right now.");
            return;
        }

        await TelegramJobService.SendDirectJobAlertToUserAsync(
            _botClient,
            chatId,
            testJob,
            sectorName: sectorName,
            header: "🚀 <b>Test Job Alert (Instant Verification)</b>");
    }

    public async Task SendOrEditCvManagementCardAsync(long chatId, int? messageId, string userId)
    {
        if (_botClient is null) return;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var profile = await db.TalentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        var hasCv = !string.IsNullOrWhiteSpace(profile?.ResumeUrl);

        var sb = new StringBuilder();
        sb.AppendLine("📄 <b>Manage Your CV</b>");
        sb.AppendLine();

        InlineKeyboardMarkup keyboard;

        if (hasCv)
        {
            var rawFileName = profile!.ResumeUrl!.Split('/').LastOrDefault() ?? "Resume.pdf";
            var cleanFileName = Regex.Replace(rawFileName, @"^\d{8}_\d{6}_", "");
            if (string.IsNullOrWhiteSpace(cleanFileName)) cleanFileName = "Resume.pdf";

            sb.AppendLine("✅ <b>Status: Active CV Connected</b>");
            sb.AppendLine($"📄 <b>Current File:</b> <code>{Escape(cleanFileName)}</code>");
            sb.AppendLine("Your resume is active for AI job compatibility scoring.");
            sb.AppendLine();
            sb.AppendLine("What would you like to do?");

            keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📥 Download Current CV", "download_cv") },
                new[] { InlineKeyboardButton.WithCallbackData("🔄 Update / Replace CV", "prompt_upload_cv") },
                new[] { InlineKeyboardButton.WithCallbackData("« Back to Dashboard", "show_dashboard") }
            });
        }
        else
        {
            sb.AppendLine("⚠️ <b>Status: No CV on File</b>");
            sb.AppendLine("You haven't uploaded a CV yet. Adding your CV unlocks AI job compatibility scoring and tailored alerts.");
            sb.AppendLine();
            sb.AppendLine("📋 <b>Requirements:</b>");
            sb.AppendLine("• Supported formats: <b>PDF (.pdf)</b> or <b>Word (.docx, .doc)</b>");
            sb.AppendLine("• Maximum file size: <b>10 MB</b>");

            keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📤 Upload CV Now", "prompt_upload_cv") },
                new[] { InlineKeyboardButton.WithCallbackData("« Back to Dashboard", "show_dashboard") }
            });
        }

        if (messageId is not null)
        {
            try
            {
                await _botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: sb.ToString(),
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to edit message for CV management, falling back to SendMessage");
            }
        }

        await _botClient.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
    }

    public async Task SendCvUploadPromptAsync(long chatId, int? messageId)
    {
        if (_botClient is null) return;

        var text =
            "📤 <b>Upload / Update Your CV</b>\n\n" +
            "Please attach and send your resume file directly in this chat!\n\n" +
            "📋 <b>File Guidelines:</b>\n" +
            "• Accepted formats: <b>PDF (.pdf)</b> or <b>Word (.docx, .doc)</b>\n" +
            "• Maximum file size: <b>10 MB</b>\n\n" +
            "<i>Simply tap the paperclip 📎 icon below, select your file, and send it. Once received, it will automatically replace your previous CV on SeraGo!</i>";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("« Back to Manage CV", "manage_cv") },
            new[] { InlineKeyboardButton.WithCallbackData("« Dashboard", "show_dashboard") }
        });

        if (messageId is not null)
        {
            try
            {
                await _botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: text,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard);
                _pendingUploadPromptMessageIds[chatId] = messageId.Value;
                return;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to edit message for CV upload prompt, falling back to SendMessage");
            }
        }

        var sentPrompt = await _botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: keyboard);
        _pendingUploadPromptMessageIds[chatId] = sentPrompt.MessageId;
    }

    public async Task DownloadAndSendCvAsync(long chatId, string userId, int? promptMessageId = null)
    {
        if (_botClient is null) return;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetService<R2StorageService>();

        if (promptMessageId is not null)
        {
            try { await _botClient.DeleteMessage(chatId, promptMessageId.Value); }
            catch (Exception ex) { _logger.LogDebug(ex, "Could not delete Manage CV prompt message {MessageId}", promptMessageId); }
        }

        var profile = await db.TalentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        if (string.IsNullOrWhiteSpace(profile?.ResumeUrl))
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📤 Upload CV Now", "prompt_upload_cv") },
                new[] { InlineKeyboardButton.WithCallbackData("« Back to Dashboard", "show_dashboard") }
            });

            await _botClient.SendMessage(
                chatId,
                "⚠️ <b>No CV on File</b>\n\nYou haven't uploaded a CV yet. Tap <b>Upload CV Now</b> to add your resume.",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
            return;
        }

        if (storage is null || !storage.IsConfigured)
        {
            await _botClient.SendMessage(chatId, "⚠️ File storage is not configured on this server.", parseMode: ParseMode.Html);
            return;
        }

        try
        {
            var (stream, contentType, fileName) = await storage.DownloadFileAsync(profile.ResumeUrl);
            await using (stream)
            {
                var rawFileName = fileName.Split('/').LastOrDefault() ?? "Resume.pdf";
                var cleanFileName = Regex.Replace(rawFileName, @"^\d{8}_\d{6}_", "");
                if (string.IsNullOrWhiteSpace(cleanFileName)) cleanFileName = "Resume.pdf";

                await _botClient.SendDocument(
                    chatId: chatId,
                    document: InputFile.FromStream(stream, cleanFileName),
                    caption: $"📄 <code>{Escape(cleanFileName)}</code>",
                    parseMode: ParseMode.Html);
            }

            await SendTalentDashboardCardAsync(
                chatId,
                userId,
                $"✅ <b>Successfully downloaded CV:</b> <code>{Escape(cleanFileName)}</code>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download and send CV directly for user {UserId}", userId);
            try
            {
                var presignedUrl = storage.GetPresignedDownloadUrlAsync(profile.ResumeUrl);
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithUrl("📥 Download CV (Secure Link)", presignedUrl) },
                    new[] { InlineKeyboardButton.WithCallbackData("« Back to Manage CV", "manage_cv") }
                });
                await _botClient.SendMessage(
                    chatId,
                    "📄 Here is a secure 15-minute download link for your CV:",
                    replyMarkup: keyboard);
            }
            catch
            {
                await _botClient.SendMessage(chatId, "⚠️ Could not retrieve your CV right now. Please try again later or check your SeraGo profile.");
            }
        }
    }

    private async Task SendUnlinkedWelcomeCardAsync(long chatId)
    {
        if (_botClient is null) return;
        var token = GenerateChatLinkingToken(chatId);
        var connectUrl = $"https://serago.pro.et/telegram/connect?chat_id={chatId}&token={token}";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🔗 Connect SeraGo Account", connectUrl) },
            new[] { InlineKeyboardButton.WithUrl("📢 Join SeraGo Job Channel", "https://t.me/SeraGo2026") },
            new[] { InlineKeyboardButton.WithUrl("🔍 Browse Latest Jobs", "https://serago.pro.et/dashboard/talent/jobs") }
        });

        var message =
            "👋 <b>Welcome to SeraGo Bot!</b>\n\n" +
            "I am your personal AI Job Alert Assistant for opportunities across Ethiopia.\n\n" +
            "<b>How to activate instant job alerts:</b>\n" +
            "Tap <b>🔗 Connect SeraGo Account</b> below to connect your account. Once connected, matched jobs will be delivered directly to this chat!\n\n" +
            "<i>Don't have an account yet? You can sign in or create a free one on the next screen in 30 seconds.</i>";

        await _botClient.SendMessage(chatId, message, parseMode: ParseMode.Html, replyMarkup: keyboard);
    }

    public async Task<(string UserId, string? FirstName)?> FindLinkedUserByChatIdAsync(long chatId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var needle1 = $"\"chatId\":{chatId}";
        var needle2 = $"\"chatId\": {chatId}";
        var needle3 = $"\"chatId\":\"{chatId}\"";
        var needle4 = $"\"chatId\": \"{chatId}\"";
        var candidates = await db.UserSettings.AsNoTracking()
            .Where(s => s.Settings.Contains(needle1) || s.Settings.Contains(needle2) || s.Settings.Contains(needle3) || s.Settings.Contains(needle4))
            .Select(s => new { s.UserId, s.Settings })
            .ToListAsync();

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.Settings)) continue;
            try
            {
                var doc = JsonDocument.Parse(candidate.Settings);
                if (doc.RootElement.TryGetProperty("telegram", out var tg))
                {
                    var isLinked = tg.TryGetProperty("linked", out var linkedProp) && linkedProp.GetBoolean();
                    if (!isLinked) continue;

                    var user = await db.Users.AsNoTracking()
                        .Where(u => u.Id == candidate.UserId)
                        .Select(u => new { u.Id, u.FirstName })
                        .FirstOrDefaultAsync();

                    if (user is not null)
                    {
                        return (user.Id, user.FirstName);
                    }
                }
            }
            catch { }
        }

        return null;
    }

    private async Task<string?> SetUserSectorAsync(long chatId, Guid sectorId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
        if (linkedUser is null) return null;

        var settingsEntity = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == linkedUser.Value.UserId);
        if (settingsEntity is null) return null;

        var sector = await db.Sectors.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sectorId && s.IsActive);
        if (sector is null) return null;

        // Switch to the chosen sector and ensure telegram alerts are active
        var newSectorIds = new List<Guid> { sectorId };
        settingsEntity.Settings = UpdateForYouSectorIdsInJson(settingsEntity.Settings, newSectorIds);
        settingsEntity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} changed alert sector to {SectorName} ({SectorId})",
            linkedUser.Value.UserId, sector.Name, sectorId);

        return sector.Name;
    }

    public async Task<bool> SetTelegramNotificationsEnabledAsync(long chatId, bool enabled)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var linkedUser = await FindLinkedUserByChatIdAsync(chatId);
        if (linkedUser is null) return false;

        var settingsEntity = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == linkedUser.Value.UserId);
        if (settingsEntity is null) return false;

        JsonNode root;
        if (string.IsNullOrWhiteSpace(settingsEntity.Settings))
        {
            root = new JsonObject();
        }
        else
        {
            try { root = JsonNode.Parse(settingsEntity.Settings) ?? new JsonObject(); }
            catch { root = new JsonObject(); }
        }

        var notifs = root["notifications"] as JsonObject;
        if (notifs is null)
        {
            notifs = new JsonObject();
            root["notifications"] = notifs;
        }

        EnsureChannelToggle(notifs, "jobAlerts", telegram: enabled);

        settingsEntity.Settings = root.ToJsonString();
        settingsEntity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} set Telegram job alert notifications to {Enabled}",
            linkedUser.Value.UserId, enabled);

        return true;
    }

    private static InlineKeyboardMarkup BuildSectorPickerKeyboard(
        List<Sector> allSectors,
        HashSet<Guid> selectedSectorIds)
    {
        var rows = new List<InlineKeyboardButton[]>();

        foreach (var s in allSectors)
        {
            var isSel = selectedSectorIds.Contains(s.Id);
            var label = isSel ? $"✅ {s.Name}" : s.Name;
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(label, $"select_sector:{s.Id}") });
        }

        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("« Back", "show_dashboard")
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
