namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// One-row state for the scraped-job sync (Id = 1): the watermark cursor that
/// makes syncs incremental. The sync only pulls scraper items whose
/// updated_at is newer than <see cref="LastSyncedAt"/>, so repeated runs cost
/// proportional to what changed, not how much data exists. The value is the
/// newest scraper-side updated_at that was processed — never "now" — so a
/// failed run simply re-processes from the old cursor (idempotent).
/// </summary>
public class SyncState
{
    public int Id { get; set; }

    /// <summary>Scraper-side cursor (UTC): items with updated_at &gt; this are pulled on the next sync.</summary>
    public DateTime LastSyncedAt { get; set; }
}
