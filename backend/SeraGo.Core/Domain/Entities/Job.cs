using SeraGo.Core.Domain.Enums;

namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// A job posting on SeraGo. This is the platform's own table (owned by EF
/// migrations) — it has no relationship to the scraper's models. Every job is
/// posted through the SeraGo API by a recruiter (or admin) and moves through
/// the <see cref="JobStatus"/> lifecycle: Draft → PendingApproval → Published,
/// with Rejected as a possible outcome of review.
/// </summary>
public class Job
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    /// <summary>Employment type (FULL_TIME, PART_TIME, ...).</summary>
    public JobType JobType { get; set; } = JobType.Other;

    public string Url { get; set; } = string.Empty;
    public string Salary { get; set; } = string.Empty;

    /// <summary>When the job opens / was published (nullable for drafts).</summary>
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? Deadline { get; set; }

    /// <summary>Draft / pending / published / rejected — the moderation lifecycle.</summary>
    public JobStatus Status { get; set; } = JobStatus.Draft;

    /// <summary>Visible to the public only when Status is Published AND IsActive.</summary>
    public bool IsActive { get; set; }

    /// <summary>The recruiter (or admin) who created the job.</summary>
    public string PostedByUserId { get; set; } = string.Empty;
    public ApplicationUser? PostedBy { get; set; }

    /// <summary>When the owner last sent the job in for review.</summary>
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public string RejectionReason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
