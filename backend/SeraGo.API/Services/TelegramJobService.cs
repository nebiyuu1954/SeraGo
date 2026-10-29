using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using Path = System.IO.Path;

namespace SeraGo.API.Services;

/// <summary>
/// Broadcasts freshly-published jobs to the Telegram channel and fans out
/// personalized job DMs to matched talents.
///
/// Always registered — when no bot token is configured the Telegram sends are
/// skipped but the in-app job alerts still work, so this fully replaces the
/// old in-app-only <c>NotifyMatchingTalentsAsync</c> path.
///
/// All work is best-effort: a Telegram failure (or a disabled channel) must
/// never fail the request that published the job.
/// </summary>
public sealed class TelegramJobService
{
    private enum RecipientSource { Sector, Ai }

    private static readonly ConcurrentDictionary<string, string> _bannerFileIdCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, byte[]> _bannerBytesCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramJobService> _logger;
    private readonly TelegramOptions _options;
    private readonly int _aiDmDelaySeconds;

    public TelegramJobService(
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramJobService> logger,
        TelegramOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
        _aiDmDelaySeconds = ParseInt(Environment.GetEnvironmentVariable("TELEGRAM_AI_DM_DELAY_SECONDS"), 300);
    }

    /// <summary>
    /// Entry point after a job becomes published/active. Enqueues the channel
    /// post (background, retried) and schedules/resolves the talent DMs.
    /// </summary>
    public async Task PublishAsync(Guid jobId, bool allowChannel, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null || job.Status != JobStatus.Published || !job.IsActive)
        {
            return;
        }

        // ── Channel broadcast ──
        // Single job published (e.g. by recruiter or admin) gets posted directly.
        if (allowChannel && _options.WantsChannel)
        {
            TryEnqueue(() => BackgroundJob.Enqueue<TelegramJobService>(s => s.PostSingleJobToChannelAsync(jobId)));
        }

        // ── Talent DMs ──
        // In ai mode the scores only exist a little after NotifyJobPublishedAsync
        // reaches SeraGo-AI, so defer the resolution; otherwise run it now.
        if (_options.AllowsAiDm)
        {
            var scheduled = TryEnqueue(() => BackgroundJob.Schedule<TelegramJobService>(
                s => s.NotifyTalentsJobAsync(jobId),
                TimeSpan.FromSeconds(_aiDmDelaySeconds)));

            // Hangfire unavailable → fall back to inline (scores may be missing,
            // but sector DMs still go out and in-app alerts are preserved).
            if (!scheduled)
            {
                await NotifyTalentsAsync(job, ct);
            }
        }
        else if (_options.AllowsSectorDm)
        {
            await NotifyTalentsAsync(job, ct);
        }

        // ── Search Engine IndexNow ping ──
        TryEnqueue(() => BackgroundJob.Enqueue<SitemapNotifierService>(
            s => s.NotifySearchEnginesAsync(new List<string> { $"{_options.JobLinkBase}/{jobId}" })));
    }

    /// <summary>
    /// Posts a single job to the public Telegram channel with duplicate guards
    /// and the prominent SeraGo-only application button.
    /// </summary>
    [Queue("telegram")]
    [AutomaticRetry(Attempts = 2)]
    public async Task PostSingleJobToChannelAsync(Guid jobId)
    {
        if (!_options.WantsChannel) return;

        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetService<ITelegramBotClient>();
        if (client is null) return;

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var job = await db.Jobs.Include(j => j.Sector).FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null || job.TelegramChannelPostedAt != null || !job.IsActive || job.Status != JobStatus.Published)
        {
            return;
        }

        var link = BuildJobLink(job);
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("See More on SeraGo", link) }
        });

        try
        {
            var message = await SendChannelPostAsync(client, job, link, keyboard);

            job.TelegramChannelPostedAt = DateTimeOffset.UtcNow;
            job.TelegramChannelMessageId = message.MessageId;
            await db.SaveChangesAsync();

            _logger.LogInformation("Posted job {JobId} ({Title}) to Telegram channel", job.Id, job.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post job {JobId} to Telegram channel", jobId);
            throw;
        }
    }

    /// <summary>
    /// Called by the scraper sync after new jobs are classified.
    /// Paces out channel posts across the 15-minute window in randomized sequence
    /// so the channel has a steady, varied stream instead of an instant blast.
    /// Also dispatches personalized sector DMs to subscribed talents.
    /// </summary>
    public Task BroadcastSyncedJobsAsync(IReadOnlyCollection<Guid> jobIds)
    {
        if (jobIds.Count == 0) return Task.CompletedTask;

        if (_options.WantsChannel)
        {
            var candidateIds = jobIds.Take(_options.ChannelMaxPerRun).OrderBy(_ => Random.Shared.Next()).ToList();
            if (candidateIds.Count > 0)
            {
                var intervalSeconds = Math.Clamp(
                    (double)_options.ChannelPacingWindowSeconds / candidateIds.Count,
                    _options.ChannelMinDelaySeconds,
                    _options.ChannelMaxDelaySeconds);

                for (var i = 0; i < candidateIds.Count; i++)
                {
                    var id = candidateIds[i];
                    var delay = TimeSpan.FromSeconds(i * intervalSeconds);
                    TryEnqueue(() => BackgroundJob.Schedule<TelegramJobService>(
                        s => s.PostSingleJobToChannelAsync(id), delay));
                }

                _logger.LogInformation(
                    "Scheduled {Count} synced jobs to Telegram channel, paced every {Interval:F1}s over {Window}s window",
                    candidateIds.Count, intervalSeconds, _options.ChannelPacingWindowSeconds);
            }
        }

        if (_options.SyncDmEnabled && (_options.AllowsAiDm || _options.AllowsSectorDm))
        {
            var syncJobIds = jobIds.ToList();
            TryEnqueue(() => BackgroundJob.Enqueue<TelegramJobService>(
                s => s.DispatchSyncedTalentDmsAsync(syncJobIds)));
        }

        // ── Search Engine IndexNow ping for batch sync ──
        var jobUrls = jobIds.Select(id => $"{_options.JobLinkBase}/{id}").ToList();
        TryEnqueue(() => BackgroundJob.Enqueue<SitemapNotifierService>(
            s => s.NotifySearchEnginesAsync(jobUrls)));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggered when an admin batch-fixes misclassified jobs.
    /// 1. Updates Telegram Channel posts in-place (EditMessageCaption) if previously posted,
    ///    or posts freshly if never posted to the channel.
    /// 2. Dispatches fresh Bot DMs to all users subscribed to the updated sectors.
    /// </summary>
    [Queue("telegram")]
    [AutomaticRetry(Attempts = 2)]
    public async Task SyncBatchFixedJobsToTelegramAsync(List<Guid> jobIds)
    {
        if (jobIds == null || jobIds.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetService<ITelegramBotClient>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var jobs = await db.Jobs
            .Include(j => j.Sector)
            .Where(j => jobIds.Contains(j.Id) && j.Status == JobStatus.Published && j.IsActive && j.SectorId != null)
            .ToListAsync();

        if (jobs.Count == 0) return;

        // ── 1. Channel Synchronization (Edit in-place or post fresh) ──
        if (_options.WantsChannel && client != null && !string.IsNullOrWhiteSpace(_options.ChannelId))
        {
            var channelChatId = new ChatId(_options.ChannelId);

            foreach (var job in jobs)
            {
                var link = BuildJobLink(job);
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithUrl("See More on SeraGo", link) }
                });

                var caption = BuildJobCaption(job, link);

                if (job.TelegramChannelMessageId.HasValue && job.TelegramChannelMessageId.Value > 0)
                {
                    try
                    {
                        await client.EditMessageCaption(
                            chatId: channelChatId,
                            messageId: (int)job.TelegramChannelMessageId.Value,
                            caption: caption,
                            parseMode: ParseMode.Html,
                            replyMarkup: keyboard);

                        _logger.LogInformation(
                            "Updated existing Telegram channel message {MessageId} for batch-fixed job {JobId} ({Title}) with sector {Sector}",
                            job.TelegramChannelMessageId.Value, job.Id, job.Title, job.SectorName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed editing channel message {MessageId} for job {JobId}. Falling back to fresh post.",
                            job.TelegramChannelMessageId.Value, job.Id);

                        try
                        {
                            var sent = await SendChannelPostAsync(client, job, link, keyboard);
                            job.TelegramChannelPostedAt = DateTimeOffset.UtcNow;
                            job.TelegramChannelMessageId = sent.MessageId;
                            await db.SaveChangesAsync();
                        }
                        catch (Exception postEx)
                        {
                            _logger.LogWarning(postEx, "Failed posting fallback channel message for job {JobId}", job.Id);
                        }
                    }
                }
                else
                {
                    // Not previously posted to channel — post fresh
                    try
                    {
                        var sent = await SendChannelPostAsync(client, job, link, keyboard);
                        job.TelegramChannelPostedAt = DateTimeOffset.UtcNow;
                        job.TelegramChannelMessageId = sent.MessageId;
                        await db.SaveChangesAsync();

                        _logger.LogInformation(
                            "Posted fresh Telegram channel message {MessageId} for batch-fixed job {JobId} ({Title})",
                            sent.MessageId, job.Id, job.Title);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed sending fresh channel post for job {JobId}", job.Id);
                    }
                }

                // Respect Telegram rate limits (1.5 seconds between channel edits/posts)
                await Task.Delay(1500);
            }
        }

        // ── 2. Bot User Direct Messages ──
        // Sends shiny new job alert DM to every user matching the corrected sector
        await DispatchSyncedTalentDmsAsync(jobIds);
    }

    /// <summary>
    /// Fallback / safety net: posts any live, published jobs not yet broadcast.
    /// Only considers jobs from the last 24 hours to prevent historical backfill floods.
    /// Shuffles order and paces posts dynamically across the configured window.
    /// </summary>
    [Queue("telegram")]
    [DisableConcurrentExecution(1800)]
    [AutomaticRetry(Attempts = 1)]
    public async Task SweepPendingChannelJobsAsync()
    {
        if (!_options.WantsChannel) return;

        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetService<ITelegramBotClient>();
        if (client is null) return;

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Auto-archive historical jobs older than 2 hours so they never flood the channel
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                @"UPDATE ""Jobs"" SET ""TelegramChannelPostedAt"" = NOW() WHERE ""TelegramChannelPostedAt"" IS NULL AND ""CreatedAt"" < NOW() - INTERVAL '2 hours'");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-archive historical Telegram channel jobs");
        }

        var cutoff = DateTimeOffset.UtcNow.AddHours(-2);
        var pending = await db.Jobs
            .Include(j => j.Sector)
            .Where(j => j.Status == JobStatus.Published && j.IsActive && j.TelegramChannelPostedAt == null &&
                        j.CreatedAt >= cutoff)
            .OrderByDescending(j => j.CreatedAt)
            .Take(_options.ChannelMaxPerRun)
            .ToListAsync();

        if (pending.Count == 0) return;

        // Randomize order so sectors/companies are varied across the window
        var shuffled = pending.OrderBy(_ => Random.Shared.Next()).ToList();
        var intervalSeconds = Math.Clamp(
            (double)_options.ChannelPacingWindowSeconds / shuffled.Count,
            _options.ChannelMinDelaySeconds,
            _options.ChannelMaxDelaySeconds);

        var posted = 0;
        foreach (var job in shuffled)
        {
            var link = BuildJobLink(job);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl("See More on SeraGo", link) }
            });

            try
            {
                var message = await SendChannelPostAsync(client, job, link, keyboard);

                job.TelegramChannelPostedAt = DateTimeOffset.UtcNow;
                job.TelegramChannelMessageId = message.MessageId;
                await db.SaveChangesAsync();
                posted++;
            }
            catch (Exception ex)
            {
                // Most likely a rate limit — stop; the next sweep continues.
                _logger.LogWarning(ex, "Channel sweep stopped at job {JobId}", job.Id);
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), CancellationToken.None);
        }

        _logger.LogInformation(
            "Telegram channel sweep posted {Posted}/{Total} pending jobs (spaced {Interval:F1}s)",
            posted, pending.Count, intervalSeconds);
    }

    /// <summary>
    /// Dispatches personalized job alerts for synced jobs to talents matching
    /// the jobs' sectors, with anti-spam caps per talent per run.
    /// Directly delivers rich job cards to connected Telegram chats.
    /// </summary>
    [Queue("telegram")]
    [AutomaticRetry(Attempts = 2)]
    public async Task DispatchSyncedTalentDmsAsync(List<Guid> jobIds)
    {
        if (jobIds.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var client = scope.ServiceProvider.GetService<ITelegramBotClient>();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var jobs = await db.Jobs
            .Include(j => j.Sector)
            .Where(j => jobIds.Contains(j.Id) && j.Status == JobStatus.Published && j.IsActive && j.SectorId != null)
            .ToListAsync();

        if (jobs.Count == 0) return;

        var jobsBySector = jobs.GroupBy(j => j.SectorId!.Value)
                               .ToDictionary(g => g.Key, g => g.ToList());

        // Include talents and admins who configured job alert preferences
        var activeUsers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && (u.UserType == UserType.Talent || u.UserType == UserType.Admin))
            .Select(u => new { u.Id })
            .ToListAsync();

        if (activeUsers.Count == 0) return;

        var activeUserIds = activeUsers.Select(u => u.Id).ToHashSet();
        var userSettings = await db.UserSettings.AsNoTracking()
            .Where(s => activeUserIds.Contains(s.UserId))
            .Select(s => new { s.UserId, s.Settings })
            .ToListAsync();

        var talentSentCounts = new Dictionary<string, int>();

        foreach (var setting in userSettings)
        {
            var preferredSectorIds = UserSettingsReader.GetForYouSectorIds(setting.Settings);
            if (preferredSectorIds.Count == 0) continue;

            long? chatId = null;
            var isTelegramAlertsEnabled = true;
            try
            {
                var doc = JsonDocument.Parse(setting.Settings);
                if (doc.RootElement.TryGetProperty("telegram", out var tg) &&
                    tg.TryGetProperty("linked", out var linked) && linked.GetBoolean() &&
                    tg.TryGetProperty("chatId", out var cid))
                {
                    chatId = cid.GetInt64();
                }

                if (doc.RootElement.TryGetProperty("notifications", out var notifs) &&
                    notifs.TryGetProperty("jobAlerts", out var ja) &&
                    ja.TryGetProperty("telegram", out var tgToggle))
                {
                    isTelegramAlertsEnabled = tgToggle.GetBoolean();
                }
            }
            catch { }

            if (chatId is null || !isTelegramAlertsEnabled)
            {
                continue; // User has unlinked or stopped Telegram notifications
            }

            var matchingJobs = new List<Job>();
            foreach (var secId in preferredSectorIds)
            {
                if (jobsBySector.TryGetValue(secId, out var secJobs))
                {
                    matchingJobs.AddRange(secJobs);
                }
            }

            if (matchingJobs.Count == 0) continue;

            var jobsToSend = matchingJobs
                .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
                .Take(_options.TalentDmMaxPerUserPerRun)
                .ToList();

            foreach (var job in jobsToSend)
            {
                var currentSent = talentSentCounts.GetValueOrDefault(setting.UserId, 0);
                if (currentSent >= _options.TalentDmMaxPerUserPerRun) break;

                var sectorName = job.Sector?.Name ?? job.SectorName ?? "Your Target Sector";

                if (chatId is not null && client is not null)
                {
                    try
                    {
                        await SendTalentJobAlertAsync(
                            client,
                            db,
                            new ChatId(chatId.Value),
                            job,
                            sectorName: sectorName);
                        talentSentCounts[setting.UserId] = currentSent + 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send job alert to chatId {ChatId} for job {JobId}", chatId, job.Id);
                    }
                }

                // Also record in-app notification so it appears on the web platform
                try
                {
                    var title = $"🎯 New Job in your Sector: {sectorName}";
                    var body = string.IsNullOrWhiteSpace(job.Location)
                        ? $"{job.Title} at {job.Company}"
                        : $"{job.Title} at {job.Company} · {job.Location}";
                    var data = JsonSerializer.Serialize(new { job_id = job.Id, sector = sectorName });

                    await notifications.CreateAsync(new NotificationService.CreateNotificationRequest(
                        UserId: setting.UserId,
                        Type: NotificationType.JobAlert,
                        Title: title,
                        Body: body,
                        Data: data,
                        SkipEmail: true,
                        SkipTelegram: true)); // Already sent directly to Telegram above
                }
                catch { }
            }
        }

        _logger.LogInformation(
            "Dispatched personalized DMs for {JobCount} synced jobs to {TalentCount} talents",
            jobs.Count, talentSentCounts.Count);
    }

    /// <summary>Hangfire worker: resolve and send the personalized DMs for a job.</summary>
    [Queue("telegram")]
    [AutomaticRetry(Attempts = 3)]
    public async Task NotifyTalentsJobAsync(Guid jobId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null || job.Status != JobStatus.Published || !job.IsActive) return;

        await NotifyTalentsAsync(job, CancellationToken.None);
    }

    /// <summary>
    /// Creates one <c>job_alert</c> notification per matched talent. Recipients
    /// are the union of the enabled match sources; the Telegram channel is only
    /// allowed for the source that matched them.
    /// </summary>
    private async Task NotifyTalentsAsync(Job job, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();

        // A Telegram DM can only happen when a bot client exists at all.
        var telegramAvailable = scope.ServiceProvider.GetService<ITelegramBotClient>() is not null;

        var recipients = new Dictionary<string, RecipientSource>();

        // ── Sector-eligible (preserves the existing in-app alert behaviour) ──
        foreach (var userId in await LoadSectorEligibleTalentsAsync(db, job, ct))
        {
            recipients[userId] = RecipientSource.Sector;
        }

        // ── AI-eligible ──
        if (_options.AllowsAiDm && job.SectorId is not null)
        {
            var matching = scope.ServiceProvider.GetService<MatchingClient>();
            if (matching is not null)
            {
                try
                {
                    var matched = await matching.GetJobMatchesAsync(
                        job.Id, _options.AiScoreThreshold, _options.JobDmMaxRecipients, ct);
                    foreach (var userId in matched)
                    {
                        recipients.TryAdd(userId, RecipientSource.Ai);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI job-match lookup failed for job {JobId}", job.Id);
                }
            }
        }

        if (recipients.Count == 0) return;

        var title = "New job matches you";
        var body = string.IsNullOrWhiteSpace(job.Location)
            ? $"{job.Title} at {job.Company}"
            : $"{job.Title} at {job.Company} · {job.Location}";
        var data = JsonSerializer.Serialize(new { job_id = job.Id, sector = job.SectorName ?? "General" });

        var sent = 0;
        foreach (var (userId, source) in recipients.Take(_options.JobDmMaxRecipients))
        {
            var telegramAllowed = telegramAvailable &&
                ((source == RecipientSource.Sector && _options.AllowsSectorDm) ||
                 (source == RecipientSource.Ai && _options.AllowsAiDm));

            try
            {
                await notifications.CreateAsync(new NotificationService.CreateNotificationRequest(
                    UserId: userId,
                    Type: NotificationType.JobAlert,
                    Title: title,
                    Body: body,
                    Data: data,
                    SkipEmail: true,                 // handled by the daily digest
                    SkipTelegram: !telegramAllowed));
                sent++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create job alert for user {UserId}", userId);
            }
        }

        _logger.LogInformation(
            "Job {JobId} published: {Count} talent alerts created (mode={Mode})",
            job.Id, sent, _options.JobDmMode);
    }

    /// <summary>
    /// Active talents whose "For You" sector list contains the job's sector.
    /// Includes admins testing/monitoring job alerts.
    /// </summary>
    private static async Task<List<string>> LoadSectorEligibleTalentsAsync(
        ApplicationDbContext db, Job job, CancellationToken ct)
    {
        var activeTalentIds = await db.Users
            .Where(u => (u.UserType == UserType.Talent || u.UserType == UserType.Admin) && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (activeTalentIds.Count == 0) return [];

        var settings = await db.UserSettings.AsNoTracking()
            .Where(s => activeTalentIds.Contains(s.UserId))
            .Select(s => new { s.UserId, s.Settings })
            .ToListAsync(ct);

        var byUser = settings.ToDictionary(s => s.UserId, s => s.Settings);

        if (job.SectorId is null)
        {
            // No sector → no targeted alert (avoids messaging every talent).
            return [];
        }

        return activeTalentIds
            .Where(id => UserSettingsReader.GetForYouSectorIds(byUser.GetValueOrDefault(id))
                .Contains(job.SectorId.Value))
            .Take(500)
            .ToList();
    }

    // ── Message building & Channel-First Delivery ───────────────────────

    /// <summary>
    /// Delivers a job alert to a talent's direct chat with the bot.
    /// Uses CHANNEL-FIRST FORWARDING: If channel posting is enabled, ensures the job is posted
    /// to the public channel first, then forwards that exact channel post to the talent.
    /// Every talent viewing the forwarded message increments the view counter on the public channel post!
    /// Also sends a companion 1-click interactive reply button.
    /// Gracefully falls back to direct photo delivery if channel is disabled or forwarding fails.
    /// </summary>
    public async Task<Message> SendTalentJobAlertAsync(
        ITelegramBotClient client,
        ApplicationDbContext db,
        ChatId chatId,
        Job job,
        string? sectorName = null,
        string? header = null,
        string? buttonText = null)
    {
        if (_options.WantsChannel && !string.IsNullOrWhiteSpace(_options.ChannelId))
        {
            var channelTarget = _options.ActiveChannelId ?? _options.ChannelId;
            var channelChatId = new ChatId(channelTarget!);

            // Ensure job is posted to the channel if not yet posted
            if (!job.TelegramChannelMessageId.HasValue || job.TelegramChannelMessageId.Value <= 0)
            {
                await EnsureJobPostedToChannelAsync(client, db, job);
            }

            if (job.TelegramChannelMessageId.HasValue && job.TelegramChannelMessageId.Value > 0)
            {
                try
                {
                    var forwarded = await client.ForwardMessage(
                        chatId: chatId,
                        fromChatId: channelChatId,
                        messageId: (int)job.TelegramChannelMessageId.Value);

                    // Send interactive companion button so user has 1-click application in bot chat
                    var jobUrl = BuildJobLink(job, _options.JobLinkBase);
                    var btnText = buttonText ?? "🚀 See More & Apply on SeraGo";
                    var replyKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithUrl(btnText, jobUrl) }
                    });

                    var resolvedSector = sectorName ?? ResolveSectorName(job) ?? "General";
                    var captionText = !string.IsNullOrWhiteSpace(header)
                        ? header
                        : $"🎯 <i>Matching alert for your sector:</i> <b>{Escape(resolvedSector)}</b>";

                    try
                    {
                        await client.SendMessage(
                            chatId: chatId,
                            text: captionText,
                            parseMode: ParseMode.Html,
                            replyParameters: new ReplyParameters { MessageId = forwarded.MessageId },
                            replyMarkup: replyKeyboard);
                    }
                    catch (Exception replyEx)
                    {
                        _logger.LogDebug(replyEx, "Could not send companion reply button for job {JobId} to chatId {ChatId}", job.Id, chatId);
                    }

                    return forwarded;
                }
                catch (Exception fwdEx)
                {
                    _logger.LogWarning(fwdEx,
                        "ForwardMessage failed for job {JobId} (MessageId: {MsgId}) to chatId {ChatId}; falling back to direct photo",
                        job.Id, job.TelegramChannelMessageId.Value, chatId);
                }
            }
        }

        // Fallback: Direct photo delivery
        return await SendDirectJobAlertToUserAsync(
            client,
            chatId,
            job,
            linkBase: _options.JobLinkBase,
            sectorName: sectorName,
            header: header,
            buttonText: buttonText,
            logger: _logger);
    }

    private async Task EnsureJobPostedToChannelAsync(ITelegramBotClient client, ApplicationDbContext db, Job job)
    {
        // Re-read latest state from DB in case another worker already posted it
        var latest = await db.Jobs.Where(j => j.Id == job.Id).Select(j => new { j.TelegramChannelMessageId, j.TelegramChannelPostedAt }).FirstOrDefaultAsync();
        if (latest?.TelegramChannelMessageId != null && latest.TelegramChannelMessageId > 0)
        {
            job.TelegramChannelMessageId = latest.TelegramChannelMessageId;
            job.TelegramChannelPostedAt = latest.TelegramChannelPostedAt;
            return;
        }

        try
        {
            var link = BuildJobLink(job);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl("See More on SeraGo", link) }
            });

            var channelMsg = await SendChannelPostAsync(client, job, link, keyboard);
            job.TelegramChannelPostedAt = DateTimeOffset.UtcNow;
            job.TelegramChannelMessageId = channelMsg.MessageId;

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Jobs\" SET \"TelegramChannelMessageId\" = {channelMsg.MessageId}, \"TelegramChannelPostedAt\" = {DateTimeOffset.UtcNow} WHERE \"Id\" = {job.Id}");

            _logger.LogInformation("Channel-First: Posted job {JobId} ({Title}) to channel (MessageId: {MsgId})",
                job.Id, job.Title, channelMsg.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post job {JobId} to channel during Channel-First preparation", job.Id);
        }
    }

    private async Task<Message> SendChannelPostAsync(
        ITelegramBotClient client,
        Job job,
        string link,
        InlineKeyboardMarkup keyboard)
    {
        var targetChannel = _options.ActiveChannelId ?? _options.ChannelId;
        return await SendDirectJobAlertToUserAsync(
            client,
            new ChatId(targetChannel!),
            job,
            linkBase: _options.JobLinkBase,
            header: string.Empty,
            logger: _logger);
    }

    public static string BuildJobCaption(Job job, string link, string? header = null)
    {
        string Build(int snippetLen, int titleLen = 0, int compLen = 0)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(header))
            {
                builder.AppendLine(header);
                builder.AppendLine();
            }
            builder.Append(BuildJobCardText(job, link, snippetLen, titleLen, compLen));
            return builder.ToString();
        }

        var caption = Build(snippetLen: 220);
        if (caption.Length > 980) caption = Build(snippetLen: 80);
        if (caption.Length > 980) caption = Build(snippetLen: 0);
        if (caption.Length > 980) caption = Build(snippetLen: 0, titleLen: 120, compLen: 60);
        return caption;
    }

    /// <summary>
    /// Sends a rich job card with verified source banner, bold title, metadata,
    /// and 1-click apply button directly to a Telegram chat.
    /// Uses file_id and in-memory byte caching with safe caption bounds to ensure
    /// reliable photo delivery without dropping banners.
    /// </summary>
    public static async Task<Message> SendDirectJobAlertToUserAsync(
        ITelegramBotClient client,
        ChatId chatId,
        Job job,
        string? linkBase = null,
        string? sectorName = null,
        string? header = null,
        string? buttonText = null,
        ILogger? logger = null)
    {
        var link = BuildJobLink(job, linkBase);
        var btnText = buttonText ?? "See More on SeraGo";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl(btnText, link) }
        });

        var caption = BuildJobCaption(job, link, header);

        var bannerFileName = GetSourceBannerFileName(job.SourceName);

        // ── 1. Telegram file_id cache ──
        // If Telegram already assigned a file_id to this banner, reuse it directly.
        // This delivers in 0ms, uses 0 bandwidth, and avoids all disk/network errors.
        if (_bannerFileIdCache.TryGetValue(bannerFileName, out var cachedFileId))
        {
            try
            {
                return await client.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromFileId(cachedFileId),
                    caption: caption,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to send photo with cached Telegram file_id for {Banner}; invalidating cache and retrying with fresh upload", bannerFileName);
                _bannerFileIdCache.TryRemove(bannerFileName, out _);
            }
        }

        // ── 2. Local banner byte cache & resolution ──
        var localBannerPath = TryResolveLocalBannerPath(bannerFileName);
        var photoUrl = GetSourceBannerUrl(job.SourceName);

        if (!_bannerBytesCache.TryGetValue(bannerFileName, out var bannerBytes))
        {
            if (localBannerPath != null && System.IO.File.Exists(localBannerPath))
            {
                try
                {
                    bannerBytes = System.IO.File.ReadAllBytes(localBannerPath);
                    _bannerBytesCache[bannerFileName] = bannerBytes;
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed reading banner bytes from {Path}; will fall back to URL", localBannerPath);
                }
            }
        }

        try
        {
            InputFile photoInput;
            MemoryStream? memStream = null;
            try
            {
                if (bannerBytes != null && bannerBytes.Length > 0)
                {
                    memStream = new MemoryStream(bannerBytes, writable: false);
                    photoInput = InputFile.FromStream(memStream, bannerFileName);
                }
                else
                {
                    photoInput = InputFile.FromUri(photoUrl);
                }

                var sentMessage = await client.SendPhoto(
                    chatId: chatId,
                    photo: photoInput,
                    caption: caption,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard);

                // Cache Telegram's assigned file_id for all future sends
                var uploadedFileId = sentMessage.Photo?.LastOrDefault()?.FileId;
                if (!string.IsNullOrEmpty(uploadedFileId))
                {
                    _bannerFileIdCache[bannerFileName] = uploadedFileId;
                }

                return sentMessage;
            }
            finally
            {
                memStream?.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to send photo for job {JobId} ({Title}) to chatId {ChatId}; falling back to SendMessage", job.Id, job.Title, chatId);
            return await client.SendMessage(
                chatId: chatId,
                text: caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
        }
    }

    public static string GetSourceBannerFileName(string? sourceName)
    {
        var normalized = (sourceName ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
        if (normalized.Contains("ethiojob"))
            return "ethiojobs_serago.jpg";
        if (normalized.Contains("hahu"))
            return "hahujobs__serago.jpg";
        if (normalized.Contains("afriwork"))
            return "afriwork_serago.jpg";
        if (normalized.Contains("geez"))
            return "geezjobs_serago.jpg";
        if (normalized.Contains("reporter"))
            return "ethioreporterjobs_serago.jpg";

        return "serago.jpg";
    }

    public static string GetSourceBannerUrl(string? sourceName)
    {
        var fileName = GetSourceBannerFileName(sourceName);
        var version = Environment.GetEnvironmentVariable("SOURCE_BANNERS_VERSION") ?? "2";
        return $"https://serago.pro.et/sources/{fileName}?v={version}";
    }

    public static string? TryResolveLocalBannerPath(string fileName)
    {
        try
        {
            var envDir = Environment.GetEnvironmentVariable("SOURCE_BANNERS_DIR");
            if (!string.IsNullOrWhiteSpace(envDir))
            {
                var candidate = System.IO.Path.Combine(envDir, fileName);
                if (System.IO.File.Exists(candidate)) return candidate;
            }

            var candidateRoots = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory,
                System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
                System.IO.Path.GetFullPath(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "..", "..")),
                System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..")),
                "/web_app",
                "/web_app/repos/SeraGo",
                "/web_app/frontend",
                @"C:\Users\Nebiyu\Desktop\Projects Projects\SeraGo"
            };

            foreach (var root in candidateRoots)
            {
                var candidates = new[]
                {
                    System.IO.Path.Combine(root, "frontend", "public", "sources", fileName),
                    System.IO.Path.Combine(root, "frontend", "dist", "sources", fileName),
                    System.IO.Path.Combine(root, "dist", "sources", fileName),
                    System.IO.Path.Combine(root, "public", "sources", fileName),
                    System.IO.Path.Combine(root, "sources", fileName),
                    System.IO.Path.Combine(root, "wwwroot", "sources", fileName),
                    System.IO.Path.Combine(root, "repos", "SeraGo", "frontend", "public", "sources", fileName),
                    System.IO.Path.Combine(root, "repos", "SeraGo", "frontend", "dist", "sources", fileName)
                };

                foreach (var c in candidates)
                {
                    if (System.IO.File.Exists(c)) return c;
                }
            }
        }
        catch
        {
            // Non-critical; fallback to URL
        }

        return null;
    }

    private string BuildJobLink(Job job)
    {
        return $"{_options.JobLinkBase}/{job.Id}?utm_source=telegram&utm_medium=channel&utm_campaign=job_alert";
    }

    public static string BuildJobLink(Job job, string? baseLink = null)
    {
        var baseUri = !string.IsNullOrWhiteSpace(baseLink)
            ? baseLink.TrimEnd('/')
            : "https://serago.pro.et/jobs";

        return $"{baseUri}/{job.Id}?utm_source=telegram&utm_medium=bot_dm&utm_campaign=job_alert";
    }

    public static string BuildJobCardText(Job job, string link, int maxSnippetLength = 220, int maxTitleLength = 0, int maxCompanyLength = 0)
    {
        var title = job.Title ?? string.Empty;
        if (maxTitleLength > 0 && title.Length > maxTitleLength)
        {
            title = title[..(maxTitleLength - 1)].TrimEnd() + "…";
        }

        var company = job.Company ?? string.Empty;
        if (maxCompanyLength > 0 && company.Length > maxCompanyLength)
        {
            company = company[..(maxCompanyLength - 1)].TrimEnd() + "…";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"<b>Title:</b> {Escape(title)}");
        sb.AppendLine();

        var meta = new List<string>();
        if (!string.IsNullOrWhiteSpace(company)) meta.Add($"🏢 <b>Company:</b> {Escape(company)}");
        if (!string.IsNullOrWhiteSpace(job.Location)) meta.Add($"📍 <b>Location:</b> {Escape(job.Location)}");
        meta.Add($"🧭 <b>Type:</b> {Humanize(job.JobType)} · {Humanize(job.WorkMode)}");
        if (!string.IsNullOrWhiteSpace(job.ExperienceLevel)) meta.Add($"🎯 <b>Experience:</b> {Escape(job.ExperienceLevel)}");
        if (!string.IsNullOrWhiteSpace(job.Salary)) meta.Add($"💰 <b>Salary:</b> {Escape(job.Salary)}");
        
        var sector = ResolveSectorName(job);
        if (!string.IsNullOrWhiteSpace(sector)) meta.Add($"🏷️ <b>Sector:</b> {Escape(sector)}");
        
        if (job.Deadline is not null) meta.Add($"⏳ <b>Closes:</b> {job.Deadline:dd MMM yyyy}");

        var sourceDisplay = !string.IsNullOrWhiteSpace(job.SourceName) ? job.SourceName : "SeraGo";
        meta.Add($"🌐 <b>Source:</b> {Escape(sourceDisplay)}");

        foreach (var line in meta) sb.AppendLine(line);

        if (maxSnippetLength > 0)
        {
            var snippet = Snippet(job.Description, maxSnippetLength);
            if (!string.IsNullOrEmpty(snippet))
            {
                sb.AppendLine();
                sb.AppendLine(snippet);
            }
        }

        sb.AppendLine();
        sb.AppendLine($"👉 <a href=\"{Escape(link)}\"><b>See More &amp; Apply on SeraGo</b></a>");
        sb.AppendLine();
        sb.AppendLine("Get Personalized Jobs through our bot @My_Serago_Bot");
        return sb.ToString();
    }

    public static string? FormatSectorName(string? raw)
    {
        var names = ExtractCleanSectorList(raw);
        return names.Count == 0 ? null : string.Join(", ", names);
    }

    public static List<string> ExtractCleanSectorList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];

        var trimmed = raw.Trim();
        var result = new List<string>();

        void TryAdd(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return;
            var clean = val.Trim();

            // If the extracted value is itself nested JSON, recursively parse it
            if ((clean.StartsWith('[') && clean.EndsWith(']')) || (clean.StartsWith('{') && clean.EndsWith('}')))
            {
                foreach (var nested in ExtractCleanSectorList(clean))
                {
                    TryAdd(nested);
                }
                return;
            }

            // Exclude leftover JSON fragments, base64 hashes, or meaningless strings
            if (clean.StartsWith('{') || clean.StartsWith('[') || clean.Length > 100) return;

            if (!result.Contains(clean, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(clean);
            }
        }

        if ((trimmed.StartsWith('[') && trimmed.EndsWith(']')) || (trimmed.StartsWith('{') && trimmed.EndsWith('}')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            TryAdd(item.GetString());
                        }
                        else if (item.ValueKind == JsonValueKind.Object)
                        {
                            string? nameVal = null;
                            foreach (var prop in item.EnumerateObject())
                            {
                                if (prop.NameEquals("name") ||
                                    prop.NameEquals("title") ||
                                    prop.NameEquals("label") ||
                                    prop.NameEquals("sector") ||
                                    prop.NameEquals("category"))
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.String)
                                    {
                                        nameVal = prop.Value.GetString();
                                        if (!string.IsNullOrWhiteSpace(nameVal)) break;
                                    }
                                }
                            }
                            TryAdd(nameVal);
                        }
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    string? nameVal = null;
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.NameEquals("name") ||
                            prop.NameEquals("title") ||
                            prop.NameEquals("label") ||
                            prop.NameEquals("sector") ||
                            prop.NameEquals("category"))
                        {
                            if (prop.Value.ValueKind == JsonValueKind.String)
                            {
                                nameVal = prop.Value.GetString();
                                if (!string.IsNullOrWhiteSpace(nameVal)) break;
                            }
                        }
                    }
                    TryAdd(nameVal);
                }
            }
            catch
            {
                return [];
            }
        }
        else
        {
            if (!trimmed.Contains("{\"") && !trimmed.Contains("[{\""))
            {
                TryAdd(trimmed);
            }
        }

        return result;
    }

    public static string? ResolveSectorName(Job job)
    {
        var candidates = new[]
        {
            job.Sector?.Name,
            job.SectorName,
            job.SubSectorName,
            job.SourceSectors,
            job.SourceCategories
        };

        foreach (var candidate in candidates)
        {
            var formatted = FormatSectorName(candidate);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                return formatted;
            }
        }

        return null;
    }

    private static readonly HashSet<string> HashtagStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "or", "the", "of", "for", "in", "to", "with", "a", "an", "at", "by",
        "id", "name", "value", "mac", "tag", "options", "title", "label", "sector", "category",
        "plc", "llc", "group", "co", "corp", "inc", "ltd"
    };

    private static string BuildSectorHashtags(string? sectorName)
    {
        var cleanDisplay = FormatSectorName(sectorName);
        if (string.IsNullOrWhiteSpace(cleanDisplay)) return "#Jobs #EthiopiaJobs";

        var clean = Regex.Replace(cleanDisplay, "[^a-zA-Z0-9 ]+", " ");
        var words = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1 &&
                        w.Length < 30 &&
                        !HashtagStopWords.Contains(w) &&
                        w.Any(char.IsLetter))
            .Take(3)
            .Select(w => $"#{char.ToUpperInvariant(w[0])}{(w.Length > 1 ? w[1..] : "")}");

        var tags = string.Join(" ", words);
        return string.IsNullOrWhiteSpace(tags) ? "#Jobs #EthiopiaJobs" : $"{tags} #Jobs #SeraGo";
    }

    private static string Snippet(string? html, int max)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = Regex.Replace(text, "\\s+", " ").Trim();
        if (text.Length > max) text = text[..max].TrimEnd() + "…";
        return Escape(text);
    }

    private static string Escape(string? text) => string.IsNullOrEmpty(text)
        ? string.Empty
        : text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    /// <summary>"FullTime" → "Full Time" for readable enum labels in the card.</summary>
    private static string Humanize(Enum value)
    {
        var name = value.ToString();
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i])) sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }

    private bool TryEnqueue(Action enqueue)
    {
        try
        {
            enqueue();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hangfire enqueue failed for a Telegram job; falling back to inline");
            return false;
        }
    }

    private static int ParseInt(string? raw, int fallback) =>
        int.TryParse(raw, out var value) && value >= 0 ? value : fallback;
}
