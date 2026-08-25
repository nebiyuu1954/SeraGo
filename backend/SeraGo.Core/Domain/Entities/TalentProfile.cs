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

    /// <summary>
    /// Per-skill visibility toggles as a JSON object. Keys are skill names,
    /// values are booleans (true = share when applying). Null or missing
    /// keys default to true. Lets talent show only relevant skills per application.
    /// </summary>
    public string SkillVisibility { get; set; } = "{}";

    /// <summary>Employment types they want — mirrors scraper JobType.</summary>
    public List<JobType> DesiredJobTypes { get; set; } = [];

    /// <summary>
    /// Canonical sectors the talent wants to see. The "For you" feed shows
    /// only jobs whose SectorId is in this list; empty means not configured.
    /// </summary>
    public List<Guid> PreferredSectorIds { get; set; } = [];

    /// <summary>Onsite / Remote / Hybrid preference.</summary>
    public WorkMode? WorkMode { get; set; }

    /// <summary>How soon they can start.</summary>
    public Availability? Availability { get; set; }

    // --------------------------------------------------- Identity / personal

    /// <summary>Date of birth (nullable for privacy).</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Street address.</summary>
    public string Address { get; set; } = string.Empty;

    // --------------------------------------------------- Education

    /// <summary>Highest education level: "HighSchool" / "Bachelors" / "Masters" / "PhD".</summary>
    public string EducationLevel { get; set; } = string.Empty;

    /// <summary>
    /// Education history as a JSON array. Each entry:
    /// { level, institution, degree, gpa, startYear, endYear? }.
    /// Variable-length — one entry per degree the talent wants to share.
    /// </summary>
    public string EducationHistory { get; set; } = "[]";

    // --------------------------------------------------- Professional context

    /// <summary>Current industry, e.g. "Technology" / "Finance".</summary>
    public string CurrentIndustry { get; set; } = string.Empty;

    /// <summary>Current profession / job title, e.g. "Software Engineer".</summary>
    public string CurrentProfession { get; set; } = string.Empty;

    /// <summary>Preferred locations as a JSON array of strings, e.g. ["Addis Ababa", "Remote"].</summary>
    public string PreferredLocations { get; set; } = "[]";

    // --------------------------------------------------- Links

    public string ResumeUrl { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string GitHubUrl { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;

    // --------------------------------------------------- Privacy

    /// <summary>
    /// Per-field visibility toggles as a JSON object. Keys are field names,
    /// values are booleans (true = share with recruiter on application).
    /// Example: { "phone": true, "dateOfBirth": false, "education": true, ... }.
    /// Null or missing keys default to true (share by default).
    /// </summary>
    public string ProfileVisibility { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
