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
            new[] { InlineKeyboardButton.WithUrl("🌐 View on SeraGo & Apply", link) }
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

        return Task.CompletedTask;
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

        // Auto-archive historical jobs older than 24 hours so they never flood the channel
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                @"UPDATE ""Jobs"" SET ""TelegramChannelPostedAt"" = NOW() WHERE ""TelegramChannelPostedAt"" IS NULL AND ""CreatedAt"" < NOW() - INTERVAL '24 hours'");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-archive historical Telegram channel jobs");
        }

        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        var pending = await db.Jobs
            .Include(j => j.Sector)
            .Where(j => j.Status == JobStatus.Published && j.IsActive && j.TelegramChannelPostedAt == null &&
                        (j.PublishedAt >= cutoff || j.CreatedAt >= cutoff))
            .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
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
                new[] { InlineKeyboardButton.WithUrl("🌐 View on SeraGo & Apply", link) }
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

        var jobs = await db.Jobs.AsNoTracking()
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
                        await SendDirectJobAlertToUserAsync(
                            client,
                            chatId.Value,
                            job,
                            linkBase: _options.JobLinkBase,
                            sectorName: sectorName);
                        talentSentCounts[setting.UserId] = currentSent + 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send direct job alert to chatId {ChatId} for job {JobId}", chatId, job.Id);
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

    // ── Message building ─────────────────────────────────────────────────

    private async Task<Message> SendChannelPostAsync(
        ITelegramBotClient client,
        Job job,
        string link,
        InlineKeyboardMarkup keyboard)
    {
        return await SendDirectJobAlertToUserAsync(
            client,
            new ChatId(_options.ChannelId!),
            job,
            linkBase: _options.JobLinkBase,
            header: string.Empty);
    }

    /// <summary>
    /// Sends a rich job card with verified source banner, bold title, metadata,
    /// and 1-click apply button directly to a Telegram chat.
    /// </summary>
    public static async Task<Message> SendDirectJobAlertToUserAsync(
        ITelegramBotClient client,
        ChatId chatId,
        Job job,
        string? linkBase = null,
        string? sectorName = null,
        string? header = null)
    {
        var baseUrl = (linkBase ?? "https://serago.pro.et/dashboard/talent/jobs").TrimEnd('/');
        var link = $"{baseUrl}/{job.Id}";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🌐 View on SeraGo & Apply", link) }
        });

        var sb = new StringBuilder();
        if (header != null)
        {
            if (!string.IsNullOrWhiteSpace(header))
            {
                sb.AppendLine(header);
                sb.AppendLine();
            }
        }
        else
        {
            var displaySector = sectorName ?? ResolveSectorName(job) ?? "Your Target Sector";
            sb.AppendLine($"🎯 <b>Job Alert for your Sector: {Escape(displaySector)}</b>");
            sb.AppendLine();
        }

        var card = BuildJobCardText(job, link, maxSnippetLength: 220);
        sb.Append(card);

        var caption = sb.ToString();
        if (caption.Length > 1020)
        {
            sb.Clear();
            if (header != null)
            {
                if (!string.IsNullOrWhiteSpace(header)) { sb.AppendLine(header); sb.AppendLine(); }
            }
            else
            {
                var displaySector = sectorName ?? ResolveSectorName(job) ?? "Your Target Sector";
                sb.AppendLine($"🎯 <b>Job Alert for your Sector: {Escape(displaySector)}</b>");
                sb.AppendLine();
            }
            sb.Append(BuildJobCardText(job, link, maxSnippetLength: 90));
            caption = sb.ToString();
            if (caption.Length > 1020) caption = caption[..1015] + "…";
        }

        var photoUrl = GetSourceBannerUrl(job.SourceName);
        try
        {
            return await client.SendPhoto(
                chatId: chatId,
                photo: InputFile.FromUri(photoUrl),
                caption: caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
        }
        catch (Exception)
        {
            return await client.SendMessage(
                chatId: chatId,
                text: caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
        }
    }

    public static string GetSourceBannerUrl(string? sourceName)
    {
        var normalized = (sourceName ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "");
        if (normalized.Contains("ethiojob"))
            return "https://serago.pro.et/sources/ethiojobs.png";
        if (normalized.Contains("hahu"))
            return "https://serago.pro.et/sources/hahujobs.png";
        if (normalized.Contains("afriwork"))
            return "https://serago.pro.et/sources/afriwork.png";
        if (normalized.Contains("geez"))
            return "https://serago.pro.et/sources/geezjobs.png";
        if (normalized.Contains("reporter"))
            return "https://serago.pro.et/sources/ethioreporterjobs.png";

        return "https://serago.pro.et/sources/serago.png";
    }

    private string BuildJobLink(Job job) => $"{_options.JobLinkBase}/{job.Id}";

    public static string BuildJobCardText(Job job, string link, int maxSnippetLength = 220)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>Title:</b> {Escape(job.Title)}");
        sb.AppendLine();

        var meta = new List<string>();
        if (!string.IsNullOrWhiteSpace(job.Company)) meta.Add($"🏢 <b>Company:</b> {Escape(job.Company)}");
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

        var snippet = Snippet(job.Description, maxSnippetLength);
        if (!string.IsNullOrEmpty(snippet))
        {
            sb.AppendLine();
            sb.AppendLine(snippet);
        }

        sb.AppendLine();
        sb.AppendLine(BuildSectorHashtags(sector));
        sb.AppendLine();
        sb.AppendLine($"🔗 <a href=\"{link}\">View full details and apply on SeraGo</a>");
        return sb.ToString();
    }

    public static string? ResolveSectorName(Job job)
    {
        if (!string.IsNullOrWhiteSpace(job.SectorName)) return job.SectorName;
        if (!string.IsNullOrWhiteSpace(job.Sector?.Name)) return job.Sector.Name;
        if (!string.IsNullOrWhiteSpace(job.SubSectorName)) return job.SubSectorName;
        if (!string.IsNullOrWhiteSpace(job.SourceSectors)) return job.SourceSectors;
        if (!string.IsNullOrWhiteSpace(job.SourceCategories)) return job.SourceCategories;
        return null;
    }

    private static string BuildSectorHashtags(string? sectorName)
    {
        if (string.IsNullOrWhiteSpace(sectorName)) return "#Jobs #EthiopiaJobs";
        var clean = Regex.Replace(sectorName, "[^a-zA-Z0-9 ]+", " ");
        var words = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1 && !string.Equals(w, "and", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .Select(w => $"#{w}");
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
