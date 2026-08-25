namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// Tracks unique views of a job's detail page. One row per (JobId, UserId)
/// — the unique index guarantees each user's view is counted exactly once
/// for the job's ViewCount.
/// </summary>
public class JobView
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }
    public Job? Job { get; set; }

    /// <summary>Authenticated user id — null for anonymous visitors.</summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>IP address of anonymous visitors — null for authenticated users.</summary>
    public string? IpAddress { get; set; }

    public DateTimeOffset ViewedAt { get; set; }
}
