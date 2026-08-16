namespace SeraGo.API.Services;

/// <summary>
/// The auto-sync timer. Runs the scraped-job sync at the UTC times in
/// SYNC_SCHEDULE (default 09:15 / 20:45 UTC — 15 minutes after the scraper's
/// GitHub Actions runs at 09:00 / 20:30 UTC), so jobs appear right after each
/// scrape. Catches up slots missed while the process was down (at most one
/// run per slot per day), and a failed sync never stops the schedule — the
/// next slot still fires. When SYNC_ENABLED is false (the default) the
/// scheduler idles and the manual admin endpoint remains the only trigger.
/// </summary>
public sealed class SyncScheduler : BackgroundService
{
    private readonly SyncOptions _options;
    private readonly ScraperDbOptions _scraperOptions;
    private readonly ScrapedJobSyncService _syncService;
    private readonly JobLifecycleCleanupService _cleanupService;
    private readonly ILogger<SyncScheduler> _logger;
    private readonly HashSet<TimeSpan> _fired = [];
    private DateTime _day = DateTime.UtcNow.Date;

    public SyncScheduler(
        SyncOptions options,
        ScraperDbOptions scraperOptions,
        ScrapedJobSyncService syncService,
        JobLifecycleCleanupService cleanupService,
        ILogger<SyncScheduler> logger)
    {
        _options = options;
        _scraperOptions = scraperOptions;
        _syncService = syncService;
        _cleanupService = cleanupService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Auto-sync disabled (SYNC_ENABLED=false) — scraped jobs are synced manually only.");
            return;
        }
        if (!_scraperOptions.IsConfigured)
        {
            _logger.LogWarning(
                "Auto-sync enabled but the scraper database is not configured "
                + "(DB_HOST/DB_NAME/DB_USER/DB_PASSWORD) — sync will not run.");
            return;
        }
        var schedule = _options.ParseSchedule();
        if (schedule.Count == 0)
        {
            _logger.LogWarning(
                "Auto-sync enabled but SYNC_SCHEDULE has no valid HH:mm UTC times — sync will not run.");
            return;
        }

        _logger.LogInformation(
            "Auto-sync scheduled at UTC: {Times}",
            string.Join(", ", schedule.Select(t => t.ToString(@"hh\:mm"))));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now.Date != _day)
                {
                    _day = now.Date;
                    _fired.Clear();
                }

                // Catch-up: fire the earliest passed slot that hasn't fired
                // today. A restart after several missed runs catches each one
                // up in order, still at most one run per slot per day.
                var pending = schedule
                    .Where(t => t <= now.TimeOfDay && !_fired.Contains(t))
                    .OrderBy(t => t)
                    .ToList();
                if (pending.Count > 0)
                {
                    await RunSync(pending[0], stoppingToken);
                    continue;
                }

                // Nothing pending — sleep until the next slot later today,
                // else tomorrow's first slot.
                var next = schedule.Where(t => t > now.TimeOfDay).DefaultIfEmpty(schedule[0]).Min();
                var nextTime = next > now.TimeOfDay
                    ? _day.Add(next)
                    : _day.AddDays(1).Add(next);
                await Task.Delay(nextTime - now, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync scheduler error — retrying in 1 minute.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task RunSync(TimeSpan slot, CancellationToken ct)
    {
        // Mark first: even if the run itself crashes, the slot counts as
        // fired so the loop can't immediately re-fire it.
        _fired.Add(slot);
        var label = slot.ToString(@"hh\:mm");
        try
        {
            var result = await _syncService.RunAsync(ct);
            if (result is null)
            {
                _logger.LogWarning(
                    "Scheduled sync {Slot} skipped — scraper database not configured.", label);
            }
            else
            {
                _logger.LogInformation(
                    "Scheduled sync {Slot} UTC — inserted={Inserted} updated={Updated} "
                    + "unchanged={Unchanged} uncategorized={Uncategorized} deactivated={Deactivated}",
                    label, result.Inserted, result.Updated, result.Unchanged,
                    result.Uncategorized, result.Deactivated);
            }

            // Weekly lifecycle cleanup (deadline + 7-day window): runs on
            // Sundays alongside the scraper's archive, deleting past-window
            // Jobs. Idempotent — the second Sunday slot deletes 0.
            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
            {
                try
                {
                    var deleted = await _cleanupService.CleanupAsync(ct);
                    if (deleted > 0)
                    {
                        _logger.LogInformation(
                            "Sunday lifecycle cleanup deleted {Deleted} past-window job(s).", deleted);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sunday lifecycle cleanup failed.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled sync {Slot} failed.", label);
        }
    }
}
