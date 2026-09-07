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

    /// <summary>Minimum salary for range-based filtering (null = not specified).</summary>
    public decimal? SalaryMin { get; set; }

    /// <summary>Maximum salary for range-based filtering (null = not specified).</summary>
    public decimal? SalaryMax { get; set; }

    /// <summary>Salary currency: "ETB" / "USD" / etc.</summary>
    public string? SalaryCurrency { get; set; }

    /// <summary>Salary period: "monthly" / "annual" / "fixed".</summary>
    public string? SalaryPeriod { get; set; }

    /// <summary>Minimum years of experience required (null = not specified).</summary>
    public int? ExperienceMinYears { get; set; }

    /// <summary>Maximum years of experience (null = open-ended).</summary>
    public int? ExperienceMaxYears { get; set; }

    /// <summary>Number of open positions for this job (default 1).</summary>
    public int NumberOfPositions { get; set; } = 1;

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

    /// <summary>
    /// Skill tags for this job (JSON array of strings, e.g. ["React", "Node.js"]).
    /// Populated from sources that provide structured skill data (Afriwork, HaHuJobs).
    /// Null for jobs without skill data or SeraGo-posted jobs.
    /// </summary>
    public string? Skills { get; set; }

    // --------------------------------------------------- Source-specific rendering fields
    //
    // These fields carry source-specific structured data through from the
    // per-site scraper models (AfriworkJob, EthioJobsJob, HaHuJob, GeezJob,
    // ReporterJob). Only the relevant source populates each field; all others
    // leave it null. The frontend uses sourceName + these fields to render
    // source-specific layouts.

    // -- Afriwork-specific --

    /// <summary>Sector names from Afriwork (JSON array), e.g. ["Technology"].</summary>
    public string? SourceSectors { get; set; }

    /// <summary>Compensation in cents (ETB) when the Afriwork API provides it.</summary>
    public int? CompensationAmountCents { get; set; }

    /// <summary>Compensation frequency: MONTHLY / FIXED / ... (Afriwork).</summary>
    public string? CompensationType { get; set; }

    /// <summary>Compensation currency: ETB / USD / ... (Afriwork).</summary>
    public string? CompensationCurrency { get; set; }

    /// <summary>Entity type: company / private_client / ... (Afriwork).</summary>
    public string? EntityType { get; set; }

    // -- EthioJobs-specific --

    /// <summary>Category/catalog list from EthioJobs (JSON array of {id, name}).</summary>
    public string? SourceCategories { get; set; }

    /// <summary>Application method: ATS / EMAIL / CAREER_PAGE_LINK / IN_PERSON (EthioJobs/HaHu).</summary>
    public string? ApplicationMethod { get; set; }

    /// <summary>Application email address (EthioJobs/HaHu).</summary>
    public string? ApplicationEmail { get; set; }

    /// <summary>Application URL / career page link (EthioJobs/HaHu).</summary>
    public string? ApplicationUrl { get; set; }

    // -- HaHuJobs-specific --

    /// <summary>Upstream aggregator source: hahujobs_telegram / hahujobs_enterprise / ... (HaHu).</summary>
    public string? UpstreamSource { get; set; }

    /// <summary>Area name from HaHuJobs, e.g. "Addis Ababa".</summary>
    public string? AreaName { get; set; }

    /// <summary>Sub-sector name from HaHuJobs, e.g. "Software Development".</summary>
    public string? SubSectorName { get; set; }

    /// <summary>Number of applicants shown on HaHuJobs.</summary>
    public int? NumberOfApplicants { get; set; }

    // -- GeezJobs-specific --

    /// <summary>Raw employment text from GeezJobs cards, e.g. "Full-time / Permanent".</summary>
    public string? EmploymentText { get; set; }

    /// <summary>GeezJobs job-time value: full_time / part_time.</summary>
    public string? JobTime { get; set; }

    /// <summary>GeezJobs site-specific job type: permanent / contract / internship / freelance / volunteer.</summary>
    public string? SiteJobType { get; set; }

    /// <summary>Raw experience text from GeezJobs cards, e.g. "3+ Years".</summary>
    public string? ExperienceText { get; set; }

    /// <summary>Maximum experience years from GeezJobs (null when open-ended).</summary>
    public int? MaxExperienceYears { get; set; }

    /// <summary>Raw posted text from GeezJobs/Reporter cards, e.g. "Posted: 3 min ago".</summary>
    public string? PostedText { get; set; }

    /// <summary>Raw deadline text from GeezJobs cards, e.g. "Deadline: September 7, 2026".</summary>
    public string? DeadlineText { get; set; }

    // -- ReporterJobs-specific --

    /// <summary>Raw job-type badge text from ReporterJobs, e.g. "Full Time".</summary>
    public string? JobTypeText { get; set; }

    /// <summary>Number of times talent have viewed this job's detail page.</summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Raw JSON from the AI job-classification service — the source of truth
    /// for what the model decided and why. Null when the job has never been
    /// classified. See <see cref="JobClassificationService"/>.
    /// </summary>
    public string? AiClassification { get; set; }

    /// <summary>When the job was last classified by the AI service (null = never).</summary>
    public DateTimeOffset? AiClassifiedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
