namespace SeraGo.API.Services;

/// <summary>
/// Configuration for the automatic scraped-job sync, read from environment
/// variables (never committed config):
///
///   SYNC_ENABLED   "true"/"1" turns the scheduler on. Default OFF — dev
///                  stays manual (the admin sync endpoint always works); flip
///                  it on Render when the auto-copy should run.
///   SYNC_SCHEDULE  comma-separated HH:mm UTC times to run the sync, timed to
///                  land shortly after each scraper run. Default "09:15,20:45"
///                  = 15 min after the GitHub Actions runs at 09:00 / 20:30
///                  UTC. Change this when the scraper's schedule changes.
/// </summary>
public sealed class SyncOptions
{
    public bool Enabled { get; }
    public string Schedule { get; }

    public SyncOptions()
    {
        var enabled = Environment.GetEnvironmentVariable("SYNC_ENABLED");
        Enabled = !string.IsNullOrWhiteSpace(enabled)
            && enabled.Trim().ToLowerInvariant() is "1" or "true" or "yes" or "on";
        Schedule = Environment.GetEnvironmentVariable("SYNC_SCHEDULE") ?? "09:15,20:45";
    }

    /// <summary>
    /// The configured HH:mm UTC times as TimeSpan-of-day, chronological order.
    /// Entries that don't parse are dropped; the list is empty when none do.
    /// </summary>
    public IReadOnlyList<TimeSpan> ParseSchedule() =>
        Schedule.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => TimeSpan.TryParseExact(
                part, @"h\:mm", System.Globalization.CultureInfo.InvariantCulture, out var t)
                ? t
                : (TimeSpan?)null)
            .Where(t => t is not null)
            .Select(t => t!.Value)
            .OrderBy(t => t)
            .ToList();
}
