namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// Cached top-K similar jobs for a job posting.
/// Maintained incrementally via a high-water mark: when new jobs are published,
/// only the newly arrived delta is evaluated against the current top-6 list.
/// </summary>
public class JobSimilarityCache
{
    /// <summary>The target job being viewed (PK and FK to Jobs).</summary>
    public Guid JobId { get; set; }
    public Job? Job { get; set; }

    /// <summary>
    /// Stored top similar jobs with their similarity scores:
    /// [{"jobId": "...", "score": 0.89}, ...]
    /// </summary>
    public string SimilarJobItemsJson { get; set; } = "[]";

    /// <summary>
    /// High-water mark: the newest PublishedAt/CreatedAt of the candidate pool
    /// evaluated when this cache was computed.
    /// </summary>
    public DateTimeOffset LastEvaluatedAt { get; set; }

    /// <summary>When this cache record was created or updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
