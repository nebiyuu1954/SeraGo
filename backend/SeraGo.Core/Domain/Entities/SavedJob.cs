namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// A user's saved (bookmarked) job. Kept as a lightweight snapshot so the
/// "how many jobs did I save" stat survives the lifecycle cleanup: when a job
/// passes its deadline + 7-day grace window it is deleted from <see cref="Job"/>,
/// but this row (with a title/company copy) stays behind for counting.
/// </summary>
public class SavedJob
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    /// <summary>The referenced job — null once it passes its lifecycle window and is deleted.</summary>
    public Guid? JobId { get; set; }
    public Job? Job { get; set; }

    // Snapshot — survives the Job row's deletion so the saved stat keeps its record.
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? SourceName { get; set; }
    public string? CompanyLogoUrl { get; set; }

    public DateTimeOffset SavedAt { get; set; }

    /// <summary>Last time the user opened their saved list (drives the "newly removed" banner).</summary>
    public DateTimeOffset? SeenAt { get; set; }
}
