using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SeraGo.Infrastructure.Context;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
    /// Handles incoming messages from Telegram users.
    /// Called by the webhook endpoint when the bot receives a message.
    /// </summary>
    public async Task HandleMessageAsync(Update update)
    {
        if (update.Message is not { Text: { } text } message)
            return;

        var chatId = message.Chat.Id;
        var userId = message.From?.Id;

        _logger.LogInformation("Telegram message from {ChatId}: {Text}", chatId, text);

        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            // Check if there's a linking token
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
                        await _botClient!.SendMessage(chatId,
                            "✅ Your SeraGo account is linked! You'll receive notifications here.\n\nUse /settings to manage preferences.",
                            parseMode: ParseMode.Html);
                        return;
                    }
                }
            }

            // Check if user is already linked
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var needle = $"\"chatId\":{chatId}";
                var alreadyLinked = await db.UserSettings.AnyAsync(s => s.Settings.Contains(needle));
                if (alreadyLinked)
                {
                    await _botClient!.SendMessage(chatId,
                        "👋 <b>Welcome back to SeraGo Bot!</b>\n\n" +
                        "✅ Your account is connected and you'll receive instant job alerts for your preferred sectors.\n\n" +
                        "Commands:\n" +
                        "/sectors - View your subscribed job sectors\n" +
                        "/settings - Manage notification preferences\n" +
                        "/unlink - Unlink this Telegram chat\n" +
                        "/help - List available commands",
                        parseMode: ParseMode.Html);
                    return;
                }
            }

            // Default welcome message
            await _botClient!.SendMessage(chatId,
                "👋 Welcome to SeraGo Bot!\n\n" +
                "To link your SeraGo account:\n" +
                "1. Open SeraGo in your browser\n" +
                "2. Go to Settings → Connected Accounts (or Notifications)\n" +
                "3. Click 'Link Telegram'\n\n" +
                "Or use /help for available commands.",
                parseMode: ParseMode.Html);
        }
        else if (text.StartsWith("/sectors", StringComparison.OrdinalIgnoreCase) ||
                 text.StartsWith("/mysectors", StringComparison.OrdinalIgnoreCase))
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var needle = $"\"chatId\":{chatId}";
            var settings = await db.UserSettings
                .Where(s => s.Settings.Contains(needle))
                .Select(s => s.Settings)
                .FirstOrDefaultAsync();

            if (settings is null)
            {
                await _botClient!.SendMessage(chatId,
                    "⚠️ Your Telegram account isn't linked to SeraGo yet.\n\nLink your account via <a href=\"https://serago.pro.et/dashboard/settings\">SeraGo Settings</a> to see your sectors and get personalized alerts.",
                    parseMode: ParseMode.Html);
                return;
            }

            var sectorIds = UserSettingsReader.GetForYouSectorIds(settings);
            if (sectorIds.Count == 0)
            {
                await _botClient!.SendMessage(chatId,
                    "ℹ️ <b>You haven't selected any preferred sectors yet.</b>\n\n" +
                    "Select your target job sectors in your SeraGo profile to receive personalized job alerts here!\n\n" +
                    "👉 <a href=\"https://serago.pro.et/dashboard/talent/profile\">Select Sectors in Profile</a>",
                    parseMode: ParseMode.Html);
                return;
            }

            var sectorNames = await db.Sectors
                .Where(s => sectorIds.Contains(s.Id))
                .Select(s => s.Name)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("📋 <b>Your Subscribed Job Sectors:</b>");
            sb.AppendLine();
            foreach (var name in sectorNames)
            {
                sb.AppendLine($"• {name}");
            }
            sb.AppendLine();
            sb.AppendLine("You'll receive personalized alerts with instant AI match insights whenever jobs are published in these sectors.");
            sb.AppendLine();
            sb.AppendLine("👉 <a href=\"https://serago.pro.et/dashboard/talent/profile\">Update Sectors in Profile</a>");

            await _botClient!.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html);
        }
        else if (text.StartsWith("/unlink", StringComparison.OrdinalIgnoreCase))
        {
            var unlinked = await UnlinkByChatIdAsync(chatId);
            await _botClient!.SendMessage(chatId,
                unlinked
                    ? "🔌 Your SeraGo account has been unlinked. You won't receive notifications here."
                    : "This Telegram chat isn't linked to a SeraGo account.",
                parseMode: ParseMode.Html);
        }
        else if (text.StartsWith("/settings", StringComparison.OrdinalIgnoreCase))
        {
            await _botClient!.SendMessage(chatId,
                "⚙️ Manage your notifications in SeraGo:\n" +
                "<a href=\"https://serago.pro.et/dashboard/settings\">Open Settings → Notifications</a>\n\n" +
                "Keep the <b>Telegram</b> toggle on for job alerts to get matched jobs here.",
                parseMode: ParseMode.Html);
        }
        else if (text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            await _botClient!.SendMessage(chatId,
                "<b>Available commands:</b>\n\n" +
                "/start - Welcome message + account linking\n" +
                "/sectors - View your subscribed job sectors\n" +
                "/settings - Manage notification preferences\n" +
                "/unlink - Unlink your SeraGo account\n" +
                "/help - Show this help message",
                parseMode: ParseMode.Html);
        }
    }
}
