using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Services;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// GET/PUT /api/account/profile — the current user's own profile.
///
/// Response shape: common fields (name, avatar, city, country) plus the
/// talent section plus a completion summary the frontend can use for
/// "finish your profile" states. Admin users have no role profile, so their
/// Talent and Completion are null.
///
/// PUT semantics: full-replace, per section. A section you send is applied
/// as-is (strings null → "", lists null → [], enums null → unset); a section
/// you omit is left untouched. Send the whole form and the result matches
/// what you sent.
/// </summary>
public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account").WithTags("Profile");

        group.MapGet("/profile", GetProfileAsync).WithOpenApi();
        group.MapPut("/profile", UpdateProfileAsync).WithOpenApi();
        // Resume → profile fields. Parse-only; the user reviews and then saves
        // through PUT /profile above, so both paths share one validator.
        group.MapPost("/profile/parse-resume", ParseResumeAsync).WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record ProfileCompletionResponse(
        bool IsComplete, int PercentComplete, List<string> MissingFields);

    public sealed record TalentProfileResponse(
        string Headline, string About, string? ExperienceLevel, int? YearsOfExperience,
        List<string> DesiredRoles, List<string> Skills, List<string> DesiredJobTypes,
        string? WorkMode, string? Availability,
        string ResumeUrl, string LinkedInUrl, string GitHubUrl, string PortfolioUrl,
        // New identity / personal fields
        string MiddleName, string? PhoneNumber, string? DateOfBirth, string Address,
        // Work experience
        string WorkExperience,
        // Education
        string EducationLevel, string EducationHistory,
        // Professional context
        string CurrentIndustry, string CurrentProfession, string PreferredLocations,
        // Privacy
        string ProfileVisibility, string SkillVisibility);

    public sealed record ProfileResponse(
        string FirstName, string MiddleName, string LastName, string Email, string Role, string AvatarUrl,
        string City, string Country, TalentProfileResponse? Talent,
        ProfileCompletionResponse? Completion,
        bool HasPassword);

    public sealed class UpdateProfileRequest
    {
        // Common — shared by every role.
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        // Role-specific — only the section matching the user's role is applied.
        public TalentProfileUpdate? Talent { get; set; }
    }

    /// <summary>
    /// Body of POST /api/account/profile/parse-resume.
    ///
    /// <c>ResumeUrl</c> carries whatever is stored on TalentProfile.ResumeUrl —
    /// an R2 object KEY, not a public URL (resumes live in a private bucket).
    /// The name is kept for symmetry with the profile field.
    /// </summary>
    public sealed class ParseResumeRequest
    {
        public string? ResumeUrl { get; set; }
    }

    public sealed class TalentProfileUpdate
    {
        public string? Headline { get; set; }
        public string? About { get; set; }
        public string? ExperienceLevel { get; set; }
        public int? YearsOfExperience { get; set; }
        public List<string>? DesiredRoles { get; set; }
        public List<string>? Skills { get; set; }
        public List<string>? DesiredJobTypes { get; set; }
        public string? WorkMode { get; set; }
        public string? Availability { get; set; }
        public string? ResumeUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? GitHubUrl { get; set; }
        public string? PortfolioUrl { get; set; }

        // Identity / personal
        public string? PhoneNumber { get; set; }
        public string? DateOfBirth { get; set; }  // ISO date string yyyy-MM-dd
        public string? Address { get; set; }

        // Work experience
        public string? WorkExperience { get; set; }  // JSON array

        // Education
        public string? EducationLevel { get; set; }
        public string? EducationHistory { get; set; }  // JSON array

        // Professional context
        public string? CurrentIndustry { get; set; }
        public string? CurrentProfession { get; set; }
        public string? PreferredLocations { get; set; }  // JSON array

        // Privacy
        public string? ProfileVisibility { get; set; }  // JSON object
        public string? SkillVisibility { get; set; }  // JSON object per-skill toggles
    }

    // -------------------------------------------------------------- Handlers

    [Authorize]
    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await BuildResponseAsync(user, db));
    }

    [Authorize]
    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        // Common fields (null = leave unchanged for strings shared with the user row).
        if (request.FirstName is not null) user.FirstName = request.FirstName.Trim();
        if (request.MiddleName is not null) user.MiddleName = request.MiddleName.Trim();
        if (request.LastName is not null) user.LastName = request.LastName.Trim();
        if (request.AvatarUrl is not null) user.AvatarUrl = request.AvatarUrl.Trim();
        if (request.City is not null) user.City = request.City.Trim();
        if (request.Country is not null) user.Country = request.Country.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        switch (user.UserType)
        {
            case UserType.Talent when request.Talent is not null:
            {
                var profile = await db.TalentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id)
                    ?? AddTalentProfile(db, user.Id);

                // Validate enums first so a bad request never half-applies.
                if (!TryParseEnum<ExperienceLevel>(request.Talent.ExperienceLevel, out var experienceLevel))
                {
                    return EnumError(typeof(ExperienceLevel), request.Talent.ExperienceLevel);
                }
                if (!TryParseEnum<WorkMode>(request.Talent.WorkMode, out var workMode))
                {
                    return EnumError(typeof(WorkMode), request.Talent.WorkMode);
                }
                if (!TryParseEnum<Availability>(request.Talent.Availability, out var availability))
                {
                    return EnumError(typeof(Availability), request.Talent.Availability);
                }
                if (request.Talent.YearsOfExperience is < 0)
                {
                    return Results.Problem(
                        "YearsOfExperience must be zero or greater.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                // Only overwrite fields the caller actually sent (null = leave unchanged).
                if (request.Talent.Headline is not null) profile.Headline = request.Talent.Headline.Trim();
                if (request.Talent.About is not null) profile.About = request.Talent.About.Trim();
                if (request.Talent.ExperienceLevel is not null) profile.ExperienceLevel = experienceLevel;
                if (request.Talent.YearsOfExperience is not null) profile.YearsOfExperience = request.Talent.YearsOfExperience;
                if (request.Talent.DesiredRoles is not null) profile.DesiredRoles = request.Talent.DesiredRoles;
                if (request.Talent.Skills is not null) profile.Skills = request.Talent.Skills;
                if (request.Talent.DesiredJobTypes is not null)
                {
                    profile.DesiredJobTypes = NormalizeJobTypes(request.Talent.DesiredJobTypes, out var jobTypeError);
                    if (jobTypeError is not null)
                    {
                        return Results.Problem(jobTypeError, statusCode: StatusCodes.Status400BadRequest);
                    }
                }
                if (request.Talent.WorkMode is not null) profile.WorkMode = workMode;
                if (request.Talent.Availability is not null) profile.Availability = availability;
                if (request.Talent.ResumeUrl is not null) profile.ResumeUrl = request.Talent.ResumeUrl.Trim();
                if (request.Talent.LinkedInUrl is not null) profile.LinkedInUrl = request.Talent.LinkedInUrl.Trim();
                if (request.Talent.GitHubUrl is not null) profile.GitHubUrl = request.Talent.GitHubUrl.Trim();
                if (request.Talent.PortfolioUrl is not null) profile.PortfolioUrl = request.Talent.PortfolioUrl.Trim();

                // Identity / personal
                if (request.Talent.PhoneNumber is not null)
                {
                    user.PhoneNumber = request.Talent.PhoneNumber.Trim();
                }
                if (DateOnly.TryParse(request.Talent.DateOfBirth, out var dob))
                {
                    profile.DateOfBirth = dob;
                }
                else if (request.Talent.DateOfBirth is not null)
                {
                    profile.DateOfBirth = null;
                }
                if (request.Talent.Address is not null) profile.Address = request.Talent.Address.Trim();

                // Work experience
                if (request.Talent.WorkExperience is not null) profile.WorkExperience = request.Talent.WorkExperience.Trim();

                // Education
                if (request.Talent.EducationLevel is not null) profile.EducationLevel = request.Talent.EducationLevel.Trim();
                if (request.Talent.EducationHistory is not null) profile.EducationHistory = request.Talent.EducationHistory.Trim();

                // Professional context
                if (request.Talent.CurrentIndustry is not null) profile.CurrentIndustry = request.Talent.CurrentIndustry.Trim();
                if (request.Talent.CurrentProfession is not null) profile.CurrentProfession = request.Talent.CurrentProfession.Trim();
                if (request.Talent.PreferredLocations is not null) profile.PreferredLocations = request.Talent.PreferredLocations.Trim();

                // Privacy
                if (request.Talent.ProfileVisibility is not null) profile.ProfileVisibility = request.Talent.ProfileVisibility.Trim();
                if (request.Talent.SkillVisibility is not null) profile.SkillVisibility = request.Talent.SkillVisibility.Trim();

                profile.UpdatedAt = DateTime.UtcNow;
                break;
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(await BuildResponseAsync(user, db));
    }

    /// <summary>
    /// POST /api/account/profile/parse-resume — read the caller's uploaded
    /// resume and return the profile fields that could be extracted.
    ///
    /// Parse-only: NOTHING is saved here. The talent reviews (and edits) the
    /// values and then saves them through PUT /api/account/profile, so this
    /// flow inherits exactly the same validation as a manual edit — there is
    /// deliberately no second save path to keep in sync.
    /// </summary>
    [Authorize]
    private static async Task<IResult> ParseResumeAsync(
        ParseResumeRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        R2StorageService storageService,
        MatchingClient matchingClient,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (user.UserType != UserType.Talent)
        {
            return Results.Problem(
                "Resume parsing is only available on talent profiles.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // The stored value is an R2 object KEY (resumes/{userId}/{timestamp}_{name}.pdf).
        var key = request.ResumeUrl?.Trim() ?? string.Empty;
        if (key.Length == 0)
        {
            return Results.Problem(
                "resumeUrl is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Ownership check. The other upload endpoints accept any "resumes/…"
        // key, which would otherwise let one talent have somebody else's CV
        // fetched, parsed and handed back as their own profile.
        if (!key.StartsWith($"resumes/{user.Id}/", StringComparison.Ordinal))
        {
            return Results.Problem(
                "You can only parse your own resume.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        string presignedUrl;
        try
        {
            // Short-lived (15 min) SigV4 GET. The AI service holds no R2
            // credentials — it only ever sees this URL.
            presignedUrl = storageService.GetPresignedDownloadUrlAsync(key);
        }
        catch (R2NotConfiguredException)
        {
            // Narrow catch, exactly like the upload endpoints: a genuine AWS
            // failure must not be reported as "not configured".
            return Results.Problem(
                "File uploads are not configured on this server.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await matchingClient.ParseResumeAsync(presignedUrl);

        if (result?.Profile is not null)
        {
            var activeSectors = await db.Sectors.Where(s => s.IsActive).ToListAsync();
            var matchedIds = new List<string>();
            var matchedNames = new List<string>();

            void TryAddSector(Sector? s)
            {
                if (s is not null && !matchedIds.Contains(s.Id.ToString()) && matchedIds.Count < 1)
                {
                    matchedIds.Add(s.Id.ToString());
                    matchedNames.Add(s.Name);
                }
            }

            var textPool = string.Join(" ", new[]
            {
                result.Profile.Headline,
                result.Profile.CurrentProfession,
                result.Profile.Skills != null ? string.Join(" ", result.Profile.Skills) : null,
                result.Profile.Education != null ? string.Join(" ", result.Profile.Education.Select(e => $"{e.Degree} {e.Institution}")) : null,
                result.Profile.Experience != null ? string.Join(" ", result.Profile.Experience.Select(e => $"{e.Title} {e.Description}")) : null,
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            // 1. Check domain keyword scoring across all active sectors first (high precision)
            if (!string.IsNullOrWhiteSpace(textPool))
            {
                var bestDomainMatch = activeSectors
                    .Select(s => new { Sector = s, Score = CalculateSectorWordMatch(s.Name, textPool) })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault();

                if (bestDomainMatch is not null)
                {
                    TryAddSector(bestDomainMatch.Sector);
                }
            }

            // 2. Check suggested sectors from AI / parser
            if (matchedIds.Count < 1 && result.Profile.SuggestedSectors is { Count: > 0 })
            {
                foreach (var name in result.Profile.SuggestedSectors)
                {
                    var found = activeSectors.FirstOrDefault(s =>
                        string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s.Slug, name, StringComparison.OrdinalIgnoreCase));
                    TryAddSector(found);
                    if (matchedIds.Count >= 1) break;
                }
            }

            // 3. Check current industry
            if (matchedIds.Count < 1 && !string.IsNullOrWhiteSpace(result.Profile.CurrentIndustry))
            {
                var found = activeSectors.FirstOrDefault(s =>
                    string.Equals(s.Name, result.Profile.CurrentIndustry, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.Slug, result.Profile.CurrentIndustry, StringComparison.OrdinalIgnoreCase));
                TryAddSector(found);
            }

            // 4. Validate and calculate years of experience strictly from work experience (never education)
            if (result.Profile.Experience is { Count: > 0 })
            {
                int totalMonths = 0;
                foreach (var exp in result.Profile.Experience)
                {
                    if (DateTime.TryParse(exp.StartDate, out var s) && DateTime.TryParse(exp.EndDate, out var e) && e >= s)
                    {
                        totalMonths += ((e.Year - s.Year) * 12) + (e.Month - s.Month);
                    }
                }
                int computedYears = totalMonths >= 12 ? totalMonths / 12 : (totalMonths >= 6 ? 1 : 0);
                if (result.Profile.YearsOfExperience == null || result.Profile.YearsOfExperience > computedYears + 1)
                {
                    result.Profile.YearsOfExperience = computedYears;
                    result.Profile.ExperienceLevel = computedYears < 1 ? "Entry"
                        : computedYears < 2 ? "Junior"
                        : computedYears < 5 ? "Mid"
                        : computedYears < 8 ? "Senior"
                        : "Lead";
                }
            }

            result.Profile.SuggestedSectorIds = matchedIds.Take(1).ToList();
            result.Profile.SuggestedSectors = matchedNames.Take(1).ToList();
        }

        // The AI service is optional and an unreadable resume is a normal
        // outcome, not an error the user must fix: unreachable ⇒ success:false.
        // A scanned (image-only) PDF arrives the same way with charsExtracted 0.
        return Results.Ok(result ?? new MatchingClient.ResumeParseResult { Success = false });
    }

    private static readonly Dictionary<string, string[]> SectorDomainKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Technology & IT"] = new[] { "software", "developer", "programmer", "programming", "web", "frontend", "backend", "fullstack", "full-stack", "engineer", "engineering", "computer", "code", "coding", "it", "ict", "devops", "cloud", "database", "sql", "python", "javascript", "typescript", "react", "angular", "vue", "django", "dotnet", ".net", "net 8", "c#", "java", "docker", "kubernetes", "aws", "azure", "git", "linux", "hilcoe", "microfrontend", "api", "rest" },
        ["Accounting & Finance"] = new[] { "accountant", "accounting", "auditor", "audit", "tax", "taxation", "financial", "finance", "bookkeeper", "bookkeeping", "payroll", "ledger", "cpa", "acca", "ifrs", "budget" },
        ["Banking & Insurance"] = new[] { "bank", "banking", "credit", "loan", "mortgage", "underwriter", "underwriting", "insurance", "actuary", "teller", "microfinance" },
        ["Healthcare"] = new[] { "doctor", "physician", "nurse", "nursing", "medical", "medicine", "hospital", "clinic", "clinical", "patient", "pharmacy", "pharmacist", "health", "healthcare", "surgeon", "surgery", "laboratory" },
        ["Engineering & Construction"] = new[] { "civil engineer", "mechanical engineer", "electrical engineer", "structural", "construction", "contractor", "site engineer", "architect", "architecture", "surveyor", "autocad", "building", "hvac", "plumbing" },
        ["Education & Training"] = new[] { "teacher", "teaching", "lecturer", "professor", "instructor", "pedagogy", "curriculum", "school", "tutor", "education", "trainer", "academic" },
        ["Legal"] = new[] { "lawyer", "attorney", "legal", "paralegal", "advocate", "prosecutor", "judge", "litigation", "law", "counsel", "compliance" },
        ["Human Resources"] = new[] { "human resources", "hr", "recruiter", "recruitment", "talent acquisition", "onboarding", "people operations", "personnel" },
        ["Sales & Marketing"] = new[] { "sales", "marketing", "advertisement", "advertising", "digital marketing", "seo", "brand", "branding", "sales representative", "telemarketing", "merchandising" },
        ["Design & Creative"] = new[] { "graphic designer", "ui designer", "ux designer", "ui/ux", "animator", "video editor", "illustrator", "photographer", "figma", "art director" },
        ["Logistics & Transportation"] = new[] { "logistics", "transport", "transportation", "fleet", "driver", "warehouse", "warehousing", "freight", "cargo", "shipping", "distribution", "dispatch" },
        ["Procurement & Supply Chain"] = new[] { "procurement", "purchasing", "supply chain", "sourcing", "vendor", "inventory", "buyer", "tender" },
        ["Customer Service & Support"] = new[] { "customer service", "call center", "customer support", "help desk", "client service", "customer care", "receptionist" },
        ["Business & Administration"] = new[] { "administration", "administrative", "executive assistant", "office manager", "operations manager", "secretary", "clerk", "business analyst" },
        ["Media & Communication"] = new[] { "journalist", "journalism", "reporter", "news", "editor", "broadcasting", "media", "communications", "public relations", "pr", "copywriter" },
        ["Hospitality & Tourism"] = new[] { "hotel", "hospitality", "restaurant", "waiter", "waitress", "chef", "cook", "culinary", "bartender", "catering", "tourism", "tour guide" },
        ["Manufacturing & Production"] = new[] { "manufacturing", "factory", "production line", "assembly", "plant operator", "quality control", "qa/qc", "machinist" },
        ["Agriculture & Natural Science"] = new[] { "agriculture", "agronomist", "agronomy", "farming", "farmer", "crop", "livestock", "veterinary", "botany", "forestry", "horticulture", "soil science", "biology", "chemist" },
        ["Security & Protection"] = new[] { "security guard", "surveillance", "safety officer", "patrol", "security officer", "cctv", "bodyguard" },
        ["Beauty & Grooming"] = new[] { "barber", "hair stylist", "hairdresser", "salon", "spa", "makeup", "esthetician", "cosmetology", "beautician" },
        ["Skilled & General Labor"] = new[] { "carpenter", "plumber", "electrician", "welder", "mason", "laborer", "cleaner", "janitor", "mechanic", "painter" },
        ["Social Science & Community"] = new[] { "social work", "social worker", "community development", "sociology", "humanitarian", "counselor", "psychology", "anthropology", "human rights", "social welfare" }
    };

    private static int CalculateSectorWordMatch(string sectorName, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var lowerText = text.ToLowerInvariant();

        if (SectorDomainKeywords.TryGetValue(sectorName, out var keywords))
        {
            int score = 0;
            foreach (var kw in keywords)
            {
                if (lowerText.Contains(kw))
                {
                    score += kw.Contains(' ') ? 3 : 2;
                }
            }
            if (score > 0) return score;
        }

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "with", "from", "that", "this", "have", "will", "your", "their", "work", "experience",
            "years", "using", "team", "also", "other", "into", "skills", "general", "services"
        };
        var textWords = text.Split(new[] { ' ', ',', '.', '/', '-', '&', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4 && !stopWords.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .ToHashSet();

        var sectorWords = sectorName.Split(new[] { ' ', ',', '.', '/', '-', '&', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4 && !stopWords.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .ToList();

        if (sectorWords.Count == 0) return 0;
        int matchCount = sectorWords.Count(sw => textWords.Contains(sw));
        int required = sectorWords.Count == 1 ? 1 : 2;
        return matchCount >= required ? matchCount : 0;
    }

    // --------------------------------------------------------------- Helpers

    private static TalentProfile AddTalentProfile(ApplicationDbContext db, string userId)
    {
        var profile = new TalentProfile { UserId = userId };
        db.TalentProfiles.Add(profile);
        return profile;
    }

    private static async Task<ProfileResponse> BuildResponseAsync(ApplicationUser user, ApplicationDbContext db)
    {
        TalentProfileResponse? talent = null;
        ProfileCompletionResponse? completion = null;

        if (user.UserType == UserType.Talent)
        {
            var profile = await db.TalentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile is not null)
            {
                talent = new TalentProfileResponse(
                    profile.Headline, profile.About,
                    profile.ExperienceLevel?.ToString(), profile.YearsOfExperience,
                    profile.DesiredRoles, profile.Skills,
                    profile.DesiredJobTypes.Select(j => j.ToString()).ToList(),
                    profile.WorkMode?.ToString(), profile.Availability?.ToString(),
                    profile.ResumeUrl, profile.LinkedInUrl, profile.GitHubUrl, profile.PortfolioUrl,
                    // New fields
                    user.MiddleName,
                    user.PhoneNumber,
                    profile.DateOfBirth?.ToString("yyyy-MM-dd"),
                    profile.Address,
                    profile.WorkExperience,
                    profile.EducationLevel,
                    profile.EducationHistory,
                    profile.CurrentIndustry,
                    profile.CurrentProfession,
                    profile.PreferredLocations,
                    profile.ProfileVisibility,
                    profile.SkillVisibility);
                completion = ComputeTalentCompletion(profile);
            }
        }

        return new ProfileResponse(
            user.FirstName, user.MiddleName, user.LastName, user.Email ?? string.Empty, user.UserType.ToString(),
            user.AvatarUrl, user.City, user.Country, talent, completion,
            user.PasswordHash is not null);
    }

    /// <summary>
    /// Fields the matching engine needs. Scoring lives in
    /// <see cref="ProfileCompletionCalculator"/> so the admin user-detail
    /// endpoint reports the identical percentage.
    /// </summary>
    private static ProfileCompletionResponse ComputeTalentCompletion(TalentProfile profile)
    {
        var result = ProfileCompletionCalculator.ForTalent(profile);
        return new ProfileCompletionResponse(
            result.IsComplete, result.PercentComplete, result.MissingFields);
    }

    /// <summary>Case-insensitive enum parse; null/blank stays null (field unset).
    /// Numeric strings ("3") are rejected so "3" never silently becomes Senior.</summary>
    private static bool TryParseEnum<T>(string? value, out T? parsed) where T : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (int.TryParse(value, out _))
        {
            return false;
        }

        if (Enum.TryParse(value, ignoreCase: true, out T result) && Enum.IsDefined(result))
        {
            parsed = result;
            return true;
        }

        return false;
    }

    /// <summary>Validates each requested job type and normalizes to the enum.</summary>
    private static List<JobType> NormalizeJobTypes(List<string>? values, out string? error)
    {
        error = null;
        if (values is null)
        {
            return [];
        }

        var result = new List<JobType>(values.Count);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = EnumErrorMessage(typeof(JobType), value);
                return [];
            }
            if (!TryParseEnum<JobType>(value, out var jobType))
            {
                error = EnumErrorMessage(typeof(JobType), value);
                return [];
            }
            result.Add(jobType!.Value);
        }
        return result;
    }

    private static IResult EnumError(Type enumType, string? value) =>
        Results.Problem(EnumErrorMessage(enumType, value), statusCode: StatusCodes.Status400BadRequest);

    private static string EnumErrorMessage(Type enumType, string? value) =>
        $"Invalid value '{value}' for {enumType.Name}. Valid values: {string.Join(", ", Enum.GetNames(enumType))}.";
}
