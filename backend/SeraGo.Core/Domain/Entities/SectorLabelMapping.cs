namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// One distinct mapping from a raw listing sector name (Alias) to the canonical
/// sector the AI decided (SectorId / SectorName). Built from actual LLM
/// classification decisions, one row per distinct (SectorId, Alias) — never a
/// per-job log. This is the accumulating vocabulary that both records the AI
/// decision and feeds the local fallback when the LLM fails for a later job.
///
/// Different from SectorAlias: SectorAlias is the admin-managed static alias
/// vocabulary (normalized exact-match lookups during sync). SectorLabelMapping is
/// the AI-derived observed mapping, written as jobs are classified and used as
/// the primary fallback signal.
/// </summary>
public class SectorLabelMapping
{
    public Guid Id { get; set; }

    public Guid SectorId { get; set; }
    public Sector Sector { get; set; } = null!;

    /// <summary>The canonical sector name the AI stored, e.g. "Technology & IT".</summary>
    public string SectorName { get; set; } = string.Empty;

    /// <summary>The original sector text from the listing, e.g. "Chemical & Biomedical Engineering".</summary>
    public string Alias { get; set; } = string.Empty;
}
