namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// 1:1 profile for Recruiter (employer) users — the third layer of the user
/// model. Mirrors how the scraper models companies (name, logo, sector) so a
/// future JobPost can reuse the same company shape. Only Recruiter users ever
/// get a row.
/// </summary>
public class RecruiterProfile
{
    /// <summary>Same PK as the owning user — shared-primary-key 1:1.</summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Company name, shown on every job post. Enforced non-empty when the profile is completed.</summary>
    public string CompanyName { get; set; } = string.Empty;

    public string CompanyLogoUrl { get; set; } = string.Empty;

    /// <summary>Company sector, e.g. "Marketing" — mirrors scraper sector_name.</summary>
    public string Industry { get; set; } = string.Empty;

    /// <summary>Size bucket, e.g. "1-10", "11-50", "51-200", "201-500", "500+".</summary>
    public string CompanySize { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    /// <summary>Company description shown on job posts.</summary>
    public string About { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
