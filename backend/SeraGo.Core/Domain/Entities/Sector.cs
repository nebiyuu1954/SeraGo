namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// A canonical job sector — the single standard vocabulary every source's
/// sector names are normalized to (e.g. "ICT", "Information Technology" and
/// "Software Design &amp; Development" all become "Technology &amp; IT").
/// Both jobs and talent preferences reference this table, so matching is
/// exact instead of string-comparison across different site vocabularies.
/// </summary>
public class Sector
{
    public Guid Id { get; set; }

    /// <summary>Display name, e.g. "Technology &amp; IT".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe identifier, e.g. "technology-it".</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Hidden sectors disappear from pickers and the feed (soft delete).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Raw names from scraped sites that map to this sector.</summary>
    public List<SectorAlias> Aliases { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
