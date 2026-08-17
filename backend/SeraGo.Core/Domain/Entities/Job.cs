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

    /// <summary>
    /// Where the work is performed (ONSITE / REMOTE / HYBRID) — mirrors the
    /// scraper's job_site. Only Afriwork carries this today, so jobs from
    /// other sources default to Onsite. Powers the "Remote" location filter,
    /// which also matches remote/hybrid jobs regardless of their location
    /// text (e.g. an Afriwork remote job listed in "Addis Ababa").
    /// </summary>
    public WorkMode WorkMode { get; set; } = WorkMode.Onsite;

    public string Url { get; set; } = string.Empty;
    public string Salary { get; set; } = string.Empty;

    /// <summary>When the job opens / was published (nullable for drafts).</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// When the source last refreshed (reposted) the listing — null for jobs
    /// never refreshed or posted directly on SeraGo. Display "Refreshed X ago"
    /// instead of the original "Posted" date when this is newer than
    /// <see cref="PublishedAt"/>.
    /// </summary>
    public DateTimeOffset? RefreshedAt { get; set; }

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

    // --------------------------------------------------- Source attribution
    //
    // Jobs imported from the scraper's database (sources / scraped_items)
    // carry where they came from. All null for jobs posted directly on
    // SeraGo by a recruiter.

    /// <summary>Display name of the source website, e.g. "EthioJobs" — null for SeraGo-posted jobs.</summary>
    public string? SourceName { get; set; }

    /// <summary>The original listing URL on the source website.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>The source's own id for this listing — dedup key together with <see cref="SourceName"/>.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Company logo URL from the source (GeezJobs/HaHuJobs/EthioJobs carry one).</summary>
    public string? CompanyLogoUrl { get; set; }

    /// <summary>Canonical sector this job belongs to (standardized at import). Null = uncategorized.</summary>
    public Guid? SectorId { get; set; }
    public Sector? Sector { get; set; }

    /// <summary>Canonical sector display name, e.g. "Technology &amp; IT". Kept as a string for cheap
    /// display on cards; the authoritative match key is <see cref="SectorId"/>.</summary>
    public string? SectorName { get; set; }

    /// <summary>Normalized experience level, e.g. "Entry", "Junior", "Senior" — or "3+ years".</summary>
    public string? ExperienceLevel { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
