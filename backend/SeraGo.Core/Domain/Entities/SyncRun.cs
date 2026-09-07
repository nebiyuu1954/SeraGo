namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// Persisted record of each scraped-job sync run. The sync service currently
/// only logs results; this table makes them queryable for the admin dashboard
/// (last sync) and the Scraper page (run history).
/// </summary>
public class SyncRun
{
    public int Id { get; set; }

    /// <summary>UTC timestamp when the sync completed.</summary>
    public DateTime RanAt { get; set; }

    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int Uncategorized { get; set; }
    public int Deactivated { get; set; }

    /// <summary>JSON array of unknown sector names encountered during this run.</summary>
    public string? UnknownSectors { get; set; }

    /// <summary>"manual" | "scheduler" — who triggered this run.</summary>
    public string TriggeredBy { get; set; } = "manual";
}
