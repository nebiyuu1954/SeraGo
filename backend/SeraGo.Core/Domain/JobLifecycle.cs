namespace SeraGo.Core.Domain;

/// <summary>
/// The job lifecycle contract between SeraGo and the scraper: a job stays
/// publicly visible until its deadline plus this many days of grace, then it
/// leaves the feed and the saved lists, and the weekly cleanup deletes it
/// (its scraper-side record is archived to a file first). The scraper
/// project mirrors these numbers (LIFECYCLE_GRACE_DAYS / DEFAULT_DEADLINE_DAYS
/// in core/scrapers/base.py) — keep them in sync.
/// </summary>
public static class JobLifecycle
{
    /// <summary>Days a job stays visible AFTER its deadline passes (deadline + 7).</summary>
    public const int GraceDays = 7;

    /// <summary>True while the job's visibility window (deadline + GraceDays) is still open.
    /// Jobs without a deadline are treated as always in-window (the scraper always sets one now).</summary>
    public static bool IsWithinWindow(DateTimeOffset? deadline, DateTimeOffset now) =>
        deadline is null || deadline.Value > now.AddDays(-GraceDays);

    /// <summary>The moment the job leaves the feed / saved lists: deadline + GraceDays.</summary>
    public static DateTimeOffset WindowEnd(DateTimeOffset? deadline, DateTimeOffset now) =>
        (deadline ?? now).AddDays(GraceDays);
}
