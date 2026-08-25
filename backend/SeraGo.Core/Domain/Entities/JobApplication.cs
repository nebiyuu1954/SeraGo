using SeraGo.Core.Domain.Enums;

namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// A talent's application to a Serago-posted job. Only applies to jobs where
/// <see cref="Job.SourceName"/> is null or "SeraGo" — external scraper jobs
/// redirect the applicant to the source website instead.
/// </summary>
public class JobApplication
{
    public Guid Id { get; set; }

    /// <summary>The job being applied to — must be a Serago job.</summary>
    public Guid JobId { get; set; }
    public Job? Job { get; set; }

    /// <summary>The talent who applied.</summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    /// <summary>Optional cover letter text.</summary>
    public string CoverLetter { get; set; } = string.Empty;

    /// <summary>Resume URL — defaults to the talent's profile resume if not provided.</summary>
    public string? ResumeUrl { get; set; }

    /// <summary>Application status — Pending → Reviewed → Accepted / Rejected.</summary>
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    /// <summary>When the talent submitted the application.</summary>
    public DateTimeOffset AppliedAt { get; set; }

    /// <summary>When the recruiter last updated the status.</summary>
    public DateTimeOffset? StatusUpdatedAt { get; set; }

    /// <summary>Optional recruiter notes (internal — not visible to the talent).</summary>
    public string RecruiterNotes { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot of the talent's visible profile data at the time of application.
    /// JSON object with the fields the talent chose to share (respecting
    /// ProfileVisibility). Stored so the recruiter sees what was shared then,
    /// even if the talent later changes their profile.
    /// </summary>
    public string? ProfileSnapshot { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
