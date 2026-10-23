using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SeraGo.API.Services;

public record TestSendRequest(
    string? TargetOverride = null, // "Channel" | "BotDm"
    string? TargetType = null,     // "test_channel" | "admin_chat" | "custom_chat"
    long? CustomChatId = null,
    string? SectorPersonaId = null,
    List<Guid>? MockSectorIds = null);

public record TestSendResult(
    bool Success,
    string Message,
    int SentCount = 1,
    string Mode = "Test",
    string TargetDestination = "",
    string? RenderedPreviewHtml = null,
    string? RenderedText = null);

/// <summary>
/// Hangfire recurring dispatcher and test executor for scheduled Telegram broadcasts
/// (Channel posts and personalized Bot DMs).
/// </summary>
public sealed class TelegramBroadcastDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramBroadcastDispatcher> _logger;
    private readonly TelegramOptions _options;

    public TelegramBroadcastDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramBroadcastDispatcher> logger,
        TelegramOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Evaluates all active schedules against the current Ethiopian local time (EAT / UTC+3).
    /// Executed by Hangfire recurring job every 5 minutes.
    /// </summary>
    [Queue("telegram")]
    [DisableConcurrentExecution(300)]
    [AutomaticRetry(Attempts = 1)]
    public async Task DispatchDueMessagesAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var client = scope.ServiceProvider.GetService<ITelegramBotClient>();

        if (client is null)
        {
            _logger.LogInformation("TelegramBotClient is not configured; skipping scheduled broadcasts");
            return;
        }

        var nowEat = TelegramStatsService.NowEat;
        var currentDay = nowEat.DayOfWeek.ToString();
        var currentDateStr = nowEat.ToString("yyyy-MM-dd");
        var currentTotalMinutes = nowEat.Hour * 60 + nowEat.Minute;

        var activeSchedules = await db.TelegramScheduledMessages
            .Where(m => m.IsActive)
            .ToListAsync(ct);

        if (activeSchedules.Count == 0) return;

        foreach (var message in activeSchedules)
        {
            // 1. Day / Date match
            var isDayMatch = false;
            if (!string.IsNullOrWhiteSpace(message.SpecificDate))
            {
                isDayMatch = string.Equals(message.SpecificDate.Trim(), currentDateStr, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isDayMatch = message.DaysOfWeek.Contains("*") ||
                             message.DaysOfWeek.Contains(currentDay, StringComparison.OrdinalIgnoreCase);
            }

            if (!isDayMatch) continue;

            // 2. Time match (within +/- 5 minutes of current dispatch interval)
            if (!TimeSpan.TryParse(message.ScheduledTime, out var scheduledSpan))
            {
                continue;
            }

            var schedTotalMinutes = (int)scheduledSpan.TotalMinutes;
            var diff = Math.Abs(currentTotalMinutes - schedTotalMinutes);
            if (diff > 5 && diff < (1440 - 5))
            {
                continue;
            }

            // 3. Prevent duplicate execution on the same calendar day (EAT)
            if (message.LastRunAt.HasValue)
            {
                var lastRunEat = message.LastRunAt.Value.ToOffset(TimeSpan.FromHours(3));
                if (lastRunEat.Date == nowEat.Date && Math.Abs((nowEat - lastRunEat).TotalMinutes) < 60)
                {
                    continue; // Already ran today in this time slot
                }
            }

            _logger.LogInformation("Executing due Telegram scheduled broadcast: {Title} (ID: {Id})", message.Title, message.Id);

            try
            {
                await ExecuteBroadcastAsync(scope, client, db, message, isTest: _options.MessagingTestMode, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed executing scheduled broadcast for message {Id}", message.Id);
                message.LastRunStatus = $"Failed: {ex.Message}";
                message.LastRunAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }
    }

    /// <summary>
    /// Direct test send invocation triggered from the Admin UI.
    /// </summary>
    public async Task<TestSendResult> ExecuteTestSendAsync(
        Guid messageId,
        TestSendRequest request,
        string adminUserId,
        CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var client = scope.ServiceProvider.GetService<ITelegramBotClient>();

        if (client is null)
        {
            return new TestSendResult(false, "Telegram bot is not configured or bot token is missing.");
        }

        var message = await db.TelegramScheduledMessages.FirstOrDefaultAsync(m => m.Id == messageId, ct);
        if (message is null)
        {
            return new TestSendResult(false, "Scheduled message not found.");
        }

        var statsService = scope.ServiceProvider.GetRequiredService<TelegramStatsService>();
        var messageBuilder = scope.ServiceProvider.GetRequiredService<TelegramMessageBuilder>();

        try
        {
            var isChannel = string.Equals(request.TargetOverride, "Channel", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(request.TargetType, "test_channel", StringComparison.OrdinalIgnoreCase);

            if (isChannel)
            {
                var channelTarget = _options.TestChannelId ?? _options.ChannelId;
                if (string.IsNullOrWhiteSpace(channelTarget))
                {
                    return new TestSendResult(false, "Neither TELEGRAM_TEST_CHANNEL_ID nor TELEGRAM_CHANNEL_ID is set in .env.");
                }

                var channelMsg = await BuildChannelMessageAsync(statsService, messageBuilder, message, ct);
                await SendFormattedAsync(client, new ChatId(channelTarget), channelMsg);

                var isTestMode = _options.MessagingTestMode || !string.IsNullOrWhiteSpace(_options.TestChannelId);
                return new TestSendResult(
                    true,
                    $"Successfully posted test card to {channelTarget}!",
                    1,
                    isTestMode ? "Isolated Test Channel" : "Production Channel",
                    channelTarget,
                    channelMsg.Text,
                    channelMsg.Text);
            }
            else
            {
                // Target is Bot DM (admin's chat, env test chat, or custom chat)
                long? targetChatId = request.CustomChatId;

                if (!targetChatId.HasValue && (request.TargetType == "admin_chat" || string.IsNullOrWhiteSpace(request.TargetType)))
                {
                    targetChatId = await ResolveUserChatIdAsync(db, adminUserId, ct);
                }

                if (!targetChatId.HasValue)
                {
                    targetChatId = _options.TestChatId;
                }

                if (!targetChatId.HasValue)
                {
                    return new TestSendResult(false, "Could not determine target chatId. Please link your Telegram account in Settings or specify a custom chatId.");
                }

                var mockSectors = request.MockSectorIds ?? new List<Guid>();
                if (!string.IsNullOrWhiteSpace(request.SectorPersonaId) && Guid.TryParse(request.SectorPersonaId, out var personaGuid))
                {
                    if (!mockSectors.Contains(personaGuid)) mockSectors.Add(personaGuid);
                }

                var botMsg = await BuildBotDmMessageAsync(statsService, messageBuilder, message, "Admin Tester", mockSectors, ct);
                await SendFormattedAsync(client, new ChatId(targetChatId.Value), botMsg);

                return new TestSendResult(
                    true,
                    $"Successfully delivered test Bot DM to chatId {targetChatId.Value}!",
                    1,
                    "Direct DM Test",
                    $"Chat ID: {targetChatId.Value}",
                    botMsg.Text,
                    botMsg.Text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test send failed for message {MessageId}", messageId);
            return new TestSendResult(false, $"Telegram error: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the broadcast (Channel and/or Bot DMs).
    /// </summary>
    private async Task ExecuteBroadcastAsync(
        IServiceScope scope,
        ITelegramBotClient client,
        ApplicationDbContext db,
        TelegramScheduledMessage message,
        bool isTest,
        CancellationToken ct)
    {
        var statsService = scope.ServiceProvider.GetRequiredService<TelegramStatsService>();
        var messageBuilder = scope.ServiceProvider.GetRequiredService<TelegramMessageBuilder>();

        var totalRecipients = 0;

        // ── 1. Channel Broadcast ─────────────────────────────────────────────
        if (message.Target == TelegramMessageTarget.Channel || message.Target == TelegramMessageTarget.Both)
        {
            var channelTarget = isTest
                ? (_options.TestChannelId ?? _options.ChannelId)
                : _options.ChannelId;

            if (!string.IsNullOrWhiteSpace(channelTarget))
            {
                try
                {
                    var channelMsg = await BuildChannelMessageAsync(statsService, messageBuilder, message, ct);
                    await SendFormattedAsync(client, new ChatId(channelTarget), channelMsg);
                    totalRecipients++;
                    _logger.LogInformation("Broadcasted scheduled message {Id} to channel {Channel}", message.Id, channelTarget);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed channel send for scheduled message {Id}", message.Id);
                }
            }
        }

        // ── 2. Bot DMs ───────────────────────────────────────────────────────
        if (message.Target == TelegramMessageTarget.BotDm || message.Target == TelegramMessageTarget.Both)
        {
            if (isTest)
            {
                // In Test Mode: only send to configured TestChatId
                if (_options.TestChatId.HasValue)
                {
                    try
                    {
                        var botMsg = await BuildBotDmMessageAsync(statsService, messageBuilder, message, "Test User", new List<Guid>(), ct);
                        await SendFormattedAsync(client, new ChatId(_options.TestChatId.Value), botMsg);
                        totalRecipients++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed test bot DM send for scheduled message {Id}", message.Id);
                    }
                }
            }
            else
            {
                // Production: send personalized DMs to linked talents
                var activeUsers = await db.Users.AsNoTracking()
                    .Where(u => u.IsActive && (u.UserType == UserType.Talent || u.UserType == UserType.Admin))
                    .Select(u => new { u.Id, u.FirstName })
                    .ToListAsync(ct);

                var activeUserIds = activeUsers.Select(u => u.Id).ToHashSet();
                var userSettings = await db.UserSettings.AsNoTracking()
                    .Where(s => activeUserIds.Contains(s.UserId))
                    .Select(s => new { s.UserId, s.Settings })
                    .ToListAsync(ct);

                var userNamesById = activeUsers.ToDictionary(u => u.Id, u => u.FirstName ?? "there");

                foreach (var setting in userSettings)
                {
                    var (linked, chatId, alertsEnabled) = ParseTelegramSetting(setting.Settings);
                    if (!linked || !chatId.HasValue || !alertsEnabled)
                    {
                        continue;
                    }

                    var preferredSectors = UserSettingsReader.GetForYouSectorIds(setting.Settings);
                    var firstName = userNamesById.GetValueOrDefault(setting.UserId, "there");

                    try
                    {
                        var botMsg = await BuildBotDmMessageAsync(statsService, messageBuilder, message, firstName, preferredSectors, ct);
                        await SendFormattedAsync(client, new ChatId(chatId.Value), botMsg);
                        totalRecipients++;

                        // Anti-flood throttle (40ms spacing between DMs)
                        await Task.Delay(40, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not deliver DM to chatId {ChatId}", chatId);
                    }
                }
            }
        }

        message.LastRunAt = DateTimeOffset.UtcNow;
        message.LastRunStatus = "Success";
        message.LastRunRecipientCount = totalRecipients;
        message.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static async Task<BuiltTelegramMessage> BuildChannelMessageAsync(
        TelegramStatsService stats,
        TelegramMessageBuilder builder,
        TelegramScheduledMessage message,
        CancellationToken ct)
    {
        return message.MessageType switch
        {
            TelegramMessageType.GoodMorning => builder.BuildMorningChannel(await stats.GetYesterdayStatsAsync(ct)),
            TelegramMessageType.GoodAfternoon => builder.BuildAfternoonChannel(await stats.GetMorningStatsAsync(ct)),
            TelegramMessageType.GoodNight => builder.BuildNightChannel(await stats.GetTodayFullStatsAsync(ct)),
            TelegramMessageType.CustomAd => builder.BuildAdMessage(message),
            _ => builder.BuildCustomMessage(message, null, await stats.GetTodayFullStatsAsync(ct), null)
        };
    }

    private static async Task<BuiltTelegramMessage> BuildBotDmMessageAsync(
        TelegramStatsService stats,
        TelegramMessageBuilder builder,
        TelegramScheduledMessage message,
        string userName,
        IReadOnlyCollection<Guid> sectorIds,
        CancellationToken ct)
    {
        var nowEat = TelegramStatsService.NowEat;
        var todayStartEat = new DateTimeOffset(nowEat.Year, nowEat.Month, nowEat.Day, 0, 0, 0, TimeSpan.FromHours(3));

        switch (message.MessageType)
        {
            case TelegramMessageType.GoodMorning:
                var yestStart = todayStartEat.AddDays(-1).ToUniversalTime();
                var yestEnd = todayStartEat.ToUniversalTime();
                var yestUserStats = await stats.GetUserSectorStatsAsync(sectorIds, yestStart, yestEnd, ct);
                return builder.BuildMorningBotDm(userName, yestUserStats);

            case TelegramMessageType.GoodAfternoon:
                var mornStart = todayStartEat.ToUniversalTime();
                var mornEnd = todayStartEat.AddHours(12).ToUniversalTime();
                var mornUserStats = await stats.GetUserSectorStatsAsync(sectorIds, mornStart, mornEnd, ct);
                return builder.BuildAfternoonBotDm(userName, mornUserStats);

            case TelegramMessageType.GoodNight:
                var dayStart = todayStartEat.ToUniversalTime();
                var dayEnd = nowEat.ToUniversalTime();
                var nightUserStats = await stats.GetUserSectorStatsAsync(sectorIds, dayStart, dayEnd, ct);
                return builder.BuildNightBotDm(userName, nightUserStats);

            case TelegramMessageType.CustomAd:
                return builder.BuildAdMessage(message, userName);

            default:
                var customSummary = await stats.GetUserSectorStatsAsync(sectorIds, todayStartEat.ToUniversalTime(), nowEat.ToUniversalTime(), ct);
                return builder.BuildCustomMessage(message, userName, null, customSummary);
        }
    }

    private static async Task SendFormattedAsync(
        ITelegramBotClient client,
        ChatId chatId,
        BuiltTelegramMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.BannerUrl))
        {
            try
            {
                await client.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromUri(message.BannerUrl),
                    caption: message.Text,
                    parseMode: ParseMode.Html,
                    replyMarkup: message.Keyboard);
                return;
            }
            catch
            {
                // Fall back to SendMessage if photo fails
            }
        }

        await client.SendMessage(
            chatId: chatId,
            text: message.Text,
            parseMode: ParseMode.Html,
            replyMarkup: message.Keyboard);
    }

    private static (bool Linked, long? ChatId, bool AlertsEnabled) ParseTelegramSetting(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson)) return (false, null, false);
        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            var root = doc.RootElement;

            long? chatId = null;
            var isLinked = false;
            var alertsEnabled = true;

            if (root.TryGetProperty("telegram", out var tg))
            {
                if (tg.TryGetProperty("linked", out var l) && l.GetBoolean()) isLinked = true;
                if (tg.TryGetProperty("chatId", out var cid)) chatId = cid.GetInt64();
            }

            if (root.TryGetProperty("notifications", out var notifs) &&
                notifs.TryGetProperty("jobAlerts", out var ja) &&
                ja.TryGetProperty("telegram", out var tgToggle))
            {
                alertsEnabled = tgToggle.GetBoolean();
            }

            return (isLinked, chatId, alertsEnabled);
        }
        catch
        {
            return (false, null, false);
        }
    }

    private static async Task<long?> ResolveUserChatIdAsync(ApplicationDbContext db, string userId, CancellationToken ct)
    {
        var settings = await db.UserSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (settings is null) return null;
        var (_, chatId, _) = ParseTelegramSetting(settings.Settings);
        return chatId;
    }

    /// <summary>
    /// Seeds default Morning, Afternoon, Night, and Promotional schedules if the table is empty.
    /// </summary>
    public static async Task SeedDefaultSchedulesAsync(ApplicationDbContext db)
    {
        if (await db.TelegramScheduledMessages.AnyAsync())
        {
            return;
        }

        var defaultSchedules = new List<TelegramScheduledMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "☀️ Good Morning Ethiopian Job Pulse",
                Target = TelegramMessageTarget.Both,
                MessageType = TelegramMessageType.GoodMorning,
                DaysOfWeek = "[\"*\"]", // Everyday
                ScheduledTime = "08:00",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "🌤️ Midday Fresh Opportunities Pulse",
                Target = TelegramMessageTarget.Both,
                MessageType = TelegramMessageType.GoodAfternoon,
                DaysOfWeek = "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]",
                ScheduledTime = "13:30",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "🌙 Evening Daily Job Summary & Wrap-Up",
                Target = TelegramMessageTarget.Both,
                MessageType = TelegramMessageType.GoodNight,
                DaysOfWeek = "[\"*\"]", // Everyday
                ScheduledTime = "20:30",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "🚀 SeraGo Superpowers & AI Features Spotlight",
                Target = TelegramMessageTarget.Channel,
                MessageType = TelegramMessageType.CustomAd,
                DaysOfWeek = "[\"Tuesday\",\"Friday\"]",
                ScheduledTime = "16:30",
                ButtonText = "🌟 Try SeraGo AI Matching",
                ButtonUrl = "https://serago.pro.et",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        db.TelegramScheduledMessages.AddRange(defaultSchedules);
        await db.SaveChangesAsync();
    }
}
