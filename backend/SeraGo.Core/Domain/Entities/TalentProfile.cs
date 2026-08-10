using SeraGo.Core.Domain.Enums;

namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// 1:1 profile for Talent (job seeker) users — the third layer of the user
/// model, holding everything the matching engine needs. Uses the same
/// vocabulary as the job scraper (<see cref="JobType"/>, <see cref="ExperienceLevel"/>,
/// <see cref="WorkMode"/>, skills) so candidate-to-job matching lines up
/// without translation. Only Talent users ever get a row.
/// </summary>
public class TalentProfile
{
    /// <summary>Same PK as the owning user — shared-primary-key 1:1.</summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>One-liner shown on cards, e.g. "Senior React Developer".</summary>
    public string Headline { get; set; } = string.Empty;

    /// <summary>Free-text bio / summary for recruiters.</summary>
    public string About { get; set; } = string.Empty;

    /// <summary>Seniority band (ENTRY / JUNIOR / SENIOR / ...). Null until set.</summary>
    public ExperienceLevel? ExperienceLevel { get; set; }

    /// <summary>Total years of experience.</summary>
    public int? YearsOfExperience { get; set; }

    /// <summary>Job titles the talent is open to, e.g. ["Product Designer"].</summary>
    public List<string> DesiredRoles { get; set; } = [];

    /// <summary>Skill names, e.g. ["Canva", "Adobe Illustrator"] — mirrors scraper skills.</summary>
    public List<string> Skills { get; set; } = [];

    /// <summary>Employment types they want — mirrors scraper JobType.</summary>
    public List<JobType> DesiredJobTypes { get; set; } = [];

    /// <summary>Onsite / Remote / Hybrid preference.</summary>
    public WorkMode? WorkMode { get; set; }

    /// <summary>How soon they can start.</summary>
    public Availability? Availability { get; set; }

    public string ResumeUrl { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string GitHubUrl { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
