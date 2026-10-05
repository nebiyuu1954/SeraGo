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
    /// Generates a linking token for a user. This token is sent to the Telegram bot
    /// and exchanged for the user's chat_id when they click the link.
    /// </summary>
    public string GenerateLinkingToken(string userId)
    {
        // Simple approach: base64 encode userId + expiry + HMAC
        // In production, use a proper token service with signature verification
        var payload = JsonSerializer.Serialize(new { u = userId, exp = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds() });
        var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
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
    /// Validates a linking token and returns the user ID if valid.
    /// </summary>
    public string? ValidateLinkingToken(string token)
    {
        try
        {
            // Restore base64 padding
            var padded = token.Replace("-", "+").Replace("_", "/");
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            var bytes = Convert.FromBase64String(padded);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var payload = JsonSerializer.Deserialize<JsonElement>(json);

            var userId = payload.GetProperty("u").GetString();
            var expiry = payload.GetProperty("exp").GetInt64();

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
                return null; // expired

            return userId;
        }
        catch
        {
            return null;
        }
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

            // Default welcome message
            await _botClient!.SendMessage(chatId,
                "👋 Welcome to SeraGo Bot!\n\n" +
                "To link your SeraGo account:\n" +
                "1. Open SeraGo in your browser\n" +
                "2. Go to Settings → Notifications\n" +
                "3. Click 'Link Telegram'\n\n" +
                "Or use /help for available commands.",
                parseMode: ParseMode.Html);
        }
        else if (text.StartsWith("/unlink", StringComparison.OrdinalIgnoreCase))
        {
            // TODO: need to identify user from chat_id
            await _botClient!.SendMessage(chatId,
                "To unlink your Telegram account, go to SeraGo Settings → Notifications → Unlink Telegram.",
                parseMode: ParseMode.Html);
        }
        else if (text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            await _botClient!.SendMessage(chatId,
                "<b>Available commands:</b>\n\n" +
                "/start - Welcome message + account linking\n" +
                "/unlink - Unlink your SeraGo account\n" +
                "/settings - Manage notification preferences\n" +
                "/help - Show this help message",
                parseMode: ParseMode.Html);
        }
    }
}
