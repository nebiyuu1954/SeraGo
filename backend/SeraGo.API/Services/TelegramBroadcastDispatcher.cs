using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

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

    private static string? _seragoPhotoFileId;
    private static string? _seragoVideoFileId;

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
    /// Supports exact times (e.g. "08:00", "13:00", "21:00") and randomized time ranges
    /// (e.g. "10:30-11:30", "14:30-15:30", "19:30-20:30") where Channel and Bot DMs
    /// are dispatched at different unexpectable times.
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

            // 2. Time match (handles exact times "HH:mm" and randomized ranges "HH:mm-HH:mm")
            var isTimeMatch = false;
            var sendChannel = false;
            var sendBot = false;

            if (message.ScheduledTime.Contains('-'))
            {
                var parts = message.ScheduledTime.Split('-');
                if (parts.Length == 2 &&
                    TimeSpan.TryParse(parts[0].Trim(), out var startSpan) &&
                    TimeSpan.TryParse(parts[1].Trim(), out var endSpan))
                {
                    var startMin = (int)startSpan.TotalMinutes;
                    var endMin = (int)endSpan.TotalMinutes;
                    if (endMin > startMin)
                    {
                        var span = endMin - startMin;
                        var half = span / 2;
                        var seed = Math.Abs(nowEat.Year * 10000 + nowEat.DayOfYear * 100 + message.Id.GetHashCode());

                        // Channel target in first half (e.g. min 5 to 25 of the window)
                        var channelMin = startMin + (seed % Math.Max(5, half - 5));
                        // Bot target in second half (e.g. min 30 to 55 of the window)
                        var botMin = startMin + half + ((seed / 17) % Math.Max(5, half - 5));

                        if (Math.Abs(currentTotalMinutes - channelMin) <= 3)
                        {
                            sendChannel = message.Target == TelegramMessageTarget.Channel || message.Target == TelegramMessageTarget.Both;
                        }

                        if (Math.Abs(currentTotalMinutes - botMin) <= 3)
                        {
                            sendBot = message.Target == TelegramMessageTarget.BotDm || message.Target == TelegramMessageTarget.Both;
                        }

                        isTimeMatch = sendChannel || sendBot;
                    }
                }
            }
            else if (TimeSpan.TryParse(message.ScheduledTime, out var scheduledSpan))
            {
                var schedTotalMinutes = (int)scheduledSpan.TotalMinutes;
                var diff = Math.Abs(currentTotalMinutes - schedTotalMinutes);
                if (diff <= 3 || diff >= (1440 - 3))
                {
                    isTimeMatch = true;
                    sendChannel = message.Target == TelegramMessageTarget.Channel || message.Target == TelegramMessageTarget.Both;
                    sendBot = message.Target == TelegramMessageTarget.BotDm || message.Target == TelegramMessageTarget.Both;
                }
            }

            if (!isTimeMatch) continue;

            // 3. Prevent duplicate execution in this time slot on the same day
            if (message.LastRunAt.HasValue)
            {
                var lastRunEat = message.LastRunAt.Value.ToOffset(TimeSpan.FromHours(3));
                if (lastRunEat.Date == nowEat.Date && Math.Abs((nowEat - lastRunEat).TotalMinutes) < 40)
                {
                    continue;
                }
            }

            _logger.LogInformation("Executing due Telegram scheduled broadcast: {Title} (ID: {Id}, Channel: {SendChan}, Bot: {SendBot})",
                message.Title, message.Id, sendChannel, sendBot);

            try
            {
                await ExecuteBroadcastAsync(scope, client, db, message, isTest: _options.MessagingTestMode, sendChannel, sendBot, ct);
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
        bool sendChannel,
        bool sendBot,
        CancellationToken ct)
    {
        var statsService = scope.ServiceProvider.GetRequiredService<TelegramStatsService>();
        var messageBuilder = scope.ServiceProvider.GetRequiredService<TelegramMessageBuilder>();

        var totalRecipients = 0;

        // ── 1. Channel Broadcast ─────────────────────────────────────────────
        Message? sentChannelMsg = null;
        var channelTarget = isTest
            ? (_options.TestChannelId ?? _options.ChannelId)
            : _options.ChannelId;

        if (sendChannel && !string.IsNullOrWhiteSpace(channelTarget))
        {
            try
            {
                var channelMsg = await BuildChannelMessageAsync(statsService, messageBuilder, message, ct);
                sentChannelMsg = await SendFormattedAsync(client, new ChatId(channelTarget), channelMsg);
                totalRecipients++;
                _logger.LogInformation("Broadcasted scheduled message {Id} to channel {Channel} (MessageId: {MsgId})",
                    message.Id, channelTarget, sentChannelMsg?.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed channel send for scheduled message {Id}", message.Id);
            }
        }

        // ── 2. Bot DMs ───────────────────────────────────────────────────────
        if (sendBot)
        {
            // For general announcements / ads where message was sent to channel, forward the channel post
            // so every bot user's view increments the view count on the public channel post!
            var canForwardChannelPost = sentChannelMsg != null && !string.IsNullOrWhiteSpace(channelTarget)
                && (message.MessageType == TelegramMessageType.CustomAd || message.MessageType == TelegramMessageType.CustomMessage);

            if (isTest)
            {
                if (_options.TestChatId.HasValue)
                {
                    try
                    {
                        if (canForwardChannelPost)
                        {
                            await client.ForwardMessage(
                                chatId: new ChatId(_options.TestChatId.Value),
                                fromChatId: new ChatId(channelTarget!),
                                messageId: sentChannelMsg!.MessageId,
                                cancellationToken: ct);
                            totalRecipients++;
                            _logger.LogInformation("Forwarded test channel broadcast {Id} to test chat {ChatId}",
                                message.Id, _options.TestChatId.Value);
                        }
                        else
                        {
                            var botMsg = await BuildBotDmMessageAsync(statsService, messageBuilder, message, "Test User", new List<Guid>(), ct);
                            await SendFormattedAsync(client, new ChatId(_options.TestChatId.Value), botMsg);
                            totalRecipients++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed test bot DM send for scheduled message {Id}", message.Id);
                    }
                }
            }
            else
            {
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

                    try
                    {
                        if (canForwardChannelPost)
                        {
                            // Forward the channel post directly to this user so their view increments the channel view counter!
                            await client.ForwardMessage(
                                chatId: new ChatId(chatId.Value),
                                fromChatId: new ChatId(channelTarget!),
                                messageId: sentChannelMsg!.MessageId,
                                cancellationToken: ct);
                            totalRecipients++;
                        }
                        else
                        {
                            var preferredSectors = UserSettingsReader.GetForYouSectorIds(setting.Settings);
                            var firstName = userNamesById.GetValueOrDefault(setting.UserId, "there");

                            var botMsg = await BuildBotDmMessageAsync(statsService, messageBuilder, message, firstName, preferredSectors, ct);
                            await SendFormattedAsync(client, new ChatId(chatId.Value), botMsg);
                            totalRecipients++;
                        }

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

    private static string? TryResolveLocalPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        if (File.Exists(relativePath)) return Path.GetFullPath(relativePath);

        var clean = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);

        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, clean),
            Path.Combine(Directory.GetCurrentDirectory(), clean),
            Path.Combine(AppContext.BaseDirectory, "frontend", "public", clean),
            Path.Combine(Directory.GetCurrentDirectory(), "frontend", "public", clean),
            Path.Combine(AppContext.BaseDirectory, "..", "frontend", "public", clean),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "frontend", "public", clean),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "frontend", "public", clean),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "frontend", "public", clean),
            // Server Linux paths
            Path.Combine("/web_app", "repos", "SeraGo", "frontend", "public", clean),
            Path.Combine("/web_app", "frontend", "dist", clean),
            Path.Combine("/web_app", "frontend", "public", clean),
            Path.Combine("/web_app", "frontend", clean)
        };

        foreach (var c in candidates)
        {
            try
            {
                if (File.Exists(c)) return Path.GetFullPath(c);
            }
            catch { }
        }

        return null;
    }

    private static async Task<Message?> SendFormattedAsync(
        ITelegramBotClient client,
        ChatId chatId,
        BuiltTelegramMessage message)
    {
        // Safe caption truncation (Telegram photo/video caption limit is 1024 chars)
        var caption = message.Text;
        if (caption.Length > 1000)
        {
            var cutIndex = caption.LastIndexOf('\n', 950);
            if (cutIndex <= 0) cutIndex = 950;
            caption = caption[..cutIndex] + "\n\n<i>...visit SeraGo for more listings!</i>";
        }

        // 1. Photo Message
        if (message.MediaType == TelegramMediaType.Photo)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_seragoPhotoFileId))
                {
                    return await client.SendPhoto(
                        chatId: chatId,
                        photo: InputFile.FromFileId(_seragoPhotoFileId),
                        caption: caption,
                        parseMode: ParseMode.Html,
                        replyMarkup: message.Keyboard);
                }

                var localPath = TryResolveLocalPath(message.MediaPathOrUrl ?? "sources/serago.jpg");
                if (localPath is not null)
                {
                    await using var stream = File.OpenRead(localPath);
                    var sent = await client.SendPhoto(
                        chatId: chatId,
                        photo: InputFile.FromStream(stream, Path.GetFileName(localPath)),
                        caption: caption,
                        parseMode: ParseMode.Html,
                        replyMarkup: message.Keyboard);

                    _seragoPhotoFileId = sent.Photo?.LastOrDefault()?.FileId;
                    return sent;
                }

                // Fall back to public URL
                var publicUrl = message.MediaPathOrUrl?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true
                    ? message.MediaPathOrUrl
                    : $"https://serago.pro.et/{message.MediaPathOrUrl?.TrimStart('/') ?? "sources/serago.jpg"}";

                var sentRemote = await client.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromUri(publicUrl),
                    caption: caption,
                    parseMode: ParseMode.Html,
                    replyMarkup: message.Keyboard);

                _seragoPhotoFileId = sentRemote.Photo?.LastOrDefault()?.FileId;
                return sentRemote;
            }
            catch
            {
                // Fall through to SendMessage if photo fails
            }
        }
        // 2. Video Message (Ads)
        else if (message.MediaType == TelegramMediaType.Video)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_seragoVideoFileId))
                {
                    return await client.SendVideo(
                        chatId: chatId,
                        video: InputFile.FromFileId(_seragoVideoFileId),
                        caption: caption,
                        parseMode: ParseMode.Html,
                        replyMarkup: message.Keyboard);
                }

                var localPath = TryResolveLocalPath(message.MediaPathOrUrl ?? "sources/SeraGoAD.mp4");
                if (localPath is not null)
                {
                    await using var stream = File.OpenRead(localPath);
                    var sent = await client.SendVideo(
                        chatId: chatId,
                        video: InputFile.FromStream(stream, Path.GetFileName(localPath)),
                        caption: caption,
                        parseMode: ParseMode.Html,
                        replyMarkup: message.Keyboard);

                    _seragoVideoFileId = sent.Video?.FileId;
                    return sent;
                }

                // Fall back to public URL
                var publicUrl = message.MediaPathOrUrl?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true
                    ? message.MediaPathOrUrl
                    : $"https://serago.pro.et/{message.MediaPathOrUrl?.TrimStart('/') ?? "sources/SeraGoAD.mp4"}";

                var sentRemote = await client.SendVideo(
                    chatId: chatId,
                    video: InputFile.FromUri(publicUrl),
                    caption: caption,
                    parseMode: ParseMode.Html,
                    replyMarkup: message.Keyboard);

                _seragoVideoFileId = sentRemote.Video?.FileId;
                return sentRemote;
            }
            catch
            {
                // Fall through to SendMessage if video fails
            }
        }

        // 3. Fallback: Standard text message
        return await client.SendMessage(
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
    /// Seeds default Morning, Afternoon, Night, and 3 Randomized Ad schedules.
    /// Can force reset if requested from Admin UI.
    /// </summary>
    public static async Task SeedDefaultSchedulesAsync(ApplicationDbContext db, bool forceReset = false)
    {
        if (forceReset)
        {
            var existing = await db.TelegramScheduledMessages.ToListAsync();
            db.TelegramScheduledMessages.RemoveRange(existing);
            await db.SaveChangesAsync();
        }
        else if (await db.TelegramScheduledMessages.AnyAsync())
        {
            return;
        }

        var defaultSchedules = new List<TelegramScheduledMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "☀️ Good Morning Job Report",
                Target = TelegramMessageTarget.Both,
                MessageType = TelegramMessageType.GoodMorning,
                DaysOfWeek = "[\"*\"]", // Everyday
                ScheduledTime = "08:00",
                BannerUrl = "sources/serago.jpg",
                ButtonText = "🔍 Browse Jobs on SeraGo",
                ButtonUrl = "https://serago.pro.et/login",
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
                DaysOfWeek = "[\"*\"]", // Everyday
                ScheduledTime = "13:00",
                BannerUrl = "sources/serago.jpg",
                ButtonText = "⚡ Explore Morning Jobs",
                ButtonUrl = "https://serago.pro.et/login",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "🌙 Daily Wrap-Up Report",
                Target = TelegramMessageTarget.Both,
                MessageType = TelegramMessageType.GoodNight,
                DaysOfWeek = "[\"*\"]", // Everyday
                ScheduledTime = "21:00",
                BannerUrl = "sources/serago.jpg",
                ButtonText = "📋 Review Today's Job Catalog",
                ButtonUrl = "https://serago.pro.et/login",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "🚀 Morning SeraGo Feature Spotlight",
                Target = TelegramMessageTarget.Both,
                MessageType = TelegramMessageType.CustomAd,
                DaysOfWeek = "[\"*\"]", // Everyday
                ScheduledTime = "10:30-11:30", // Randomized between 10:30 and 11:30
                BannerUrl = "sources/SeraGoAD.mp4",
                ButtonText = "🚀 Visit SeraGo Website",
                ButtonUrl = "https://serago.pro.et/login",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "🚀 Afternoon SeraGo Feature Spotlight",
                Target = TelegramMessageTarget.Both,
                MessageType = TelegramMessageType.CustomAd,
                DaysOfWeek = "[\"*\"]", // Everyday
                ScheduledTime = "14:30-15:30", // Randomized between 14:30 and 15:30
                BannerUrl = "sources/SeraGoAD.mp4",
                ButtonText = "🚀 Visit SeraGo Website",
                ButtonUrl = "https://serago.pro.et/login",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "🚀 Evening SeraGo Feature Spotlight",
                Target = TelegramMessageTarget.Both,
                MessageType = TelegramMessageType.CustomAd,
                DaysOfWeek = "[\"*\"]", // Everyday
                ScheduledTime = "19:30-20:30", // Randomized between 19:30 and 20:30
                BannerUrl = "sources/SeraGoAD.mp4",
                ButtonText = "🚀 Visit SeraGo Website",
                ButtonUrl = "https://serago.pro.et/login",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        db.TelegramScheduledMessages.AddRange(defaultSchedules);
        await db.SaveChangesAsync();
    }
}
