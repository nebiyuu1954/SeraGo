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
/// role-specific section (Talent or Recruiter) plus a completion summary the
/// frontend can use for "finish your profile" states. Admin users have no
/// role profile, so their Talent/Recruiter and Completion are null.
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

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record ProfileCompletionResponse(
        bool IsComplete, int PercentComplete, List<string> MissingFields);

    public sealed record TalentProfileResponse(
        string Headline, string About, string? ExperienceLevel, int? YearsOfExperience,
        List<string> DesiredRoles, List<string> Skills, List<string> DesiredJobTypes,
        string? WorkMode, string? Availability, List<Guid> PreferredSectorIds,
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

    public sealed record RecruiterProfileResponse(
        string CompanyName, string Industry, string CompanySize,
        string WebsiteUrl, string About,
        // New attributes
        int? FoundedYear, string Headquarters, string PhoneNumber, string Email,
        string? CompanyType, string LinkedInUrl, string TwitterUrl,
        // Privacy
        string CompanyVisibility, bool IsCompanyPrivate);

    public sealed record ProfileResponse(
        string FirstName, string MiddleName, string LastName, string Email, string Role, string AvatarUrl,
        string City, string Country, TalentProfileResponse? Talent,
        RecruiterProfileResponse? Recruiter, ProfileCompletionResponse? Completion,
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
        public RecruiterProfileUpdate? Recruiter { get; set; }
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

        /// <summary>Canonical sector ids the talent wants in their feed.</summary>
        public List<Guid>? PreferredSectorIds { get; set; }
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

    public sealed class RecruiterProfileUpdate
    {
        public string? CompanyName { get; set; }
        public string? Industry { get; set; }
        public string? CompanySize { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? About { get; set; }
        // New attributes
        public int? FoundedYear { get; set; }
        public string? Headquarters { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? CompanyType { get; set; }  // PascalCase enum name
        public string? LinkedInUrl { get; set; }
        public string? TwitterUrl { get; set; }
        // Privacy
        public string? CompanyVisibility { get; set; }  // JSON object
        public bool? IsCompanyPrivate { get; set; }
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
        ApplicationDbContext db,
        MatchingClient matchingClient)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        // Snapshot the talent profile before update (for staleness comparison)
        Dictionary<string, object?>? previousProfile = null;
        if (user.UserType == UserType.Talent)
        {
            var existing = await db.TalentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (existing is not null)
            {
                previousProfile = new Dictionary<string, object?>
                {
                    ["city"] = user.City,
                    ["country"] = user.Country,
                    ["experienceLevel"] = existing.ExperienceLevel.ToString(),
                    ["yearsOfExperience"] = existing.YearsOfExperience,
                    ["currentIndustry"] = existing.CurrentIndustry,
                    ["currentProfession"] = existing.CurrentProfession,
                    ["workMode"] = existing.WorkMode.ToString(),
                    ["skills"] = existing.Skills,
                    ["workExperience"] = existing.WorkExperience,
                    ["educationHistory"] = existing.EducationHistory,
                };
            }
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
                if (request.Talent.PreferredSectorIds is not null)
                {
                    var ids = request.Talent.PreferredSectorIds.Distinct().ToList();
                    if (ids.Count > 0)
                    {
                        var existing = await db.Sectors
                            .Where(s => ids.Contains(s.Id))
                            .Select(s => s.Id)
                            .ToListAsync();
                        if (existing.Count != ids.Count)
                        {
                            return Results.Problem(
                                "One or more preferredSectorIds don't exist.",
                                statusCode: StatusCodes.Status400BadRequest);
                        }
                    }
                    profile.PreferredSectorIds = ids;
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

            case UserType.Recruiter when request.Recruiter is not null:
            {
                var profile = await db.RecruiterProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id)
                    ?? AddRecruiterProfile(db, user.Id);

                // CompanyName is the one field that must be present the moment a
                // recruiter touches their profile — job posts depend on it.
                if (string.IsNullOrWhiteSpace(request.Recruiter.CompanyName))
                {
                    return Results.Problem(
                        "CompanyName is required on a recruiter profile.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                profile.CompanyName = request.Recruiter.CompanyName.Trim();
                if (request.Recruiter.Industry is not null) profile.Industry = request.Recruiter.Industry.Trim();
                if (request.Recruiter.CompanySize is not null) profile.CompanySize = request.Recruiter.CompanySize.Trim();
                if (request.Recruiter.WebsiteUrl is not null) profile.WebsiteUrl = request.Recruiter.WebsiteUrl.Trim();
                if (request.Recruiter.About is not null) profile.About = request.Recruiter.About.Trim();

                // New attributes
                profile.FoundedYear = request.Recruiter.FoundedYear;
                if (request.Recruiter.Headquarters is not null) profile.Headquarters = request.Recruiter.Headquarters.Trim();
                if (request.Recruiter.PhoneNumber is not null) profile.PhoneNumber = request.Recruiter.PhoneNumber.Trim();
                if (request.Recruiter.Email is not null) profile.Email = request.Recruiter.Email.Trim();
                if (TryParseEnum<CompanyType>(request.Recruiter.CompanyType, out var companyType))
                {
                    profile.CompanyType = companyType;
                }
                else
                {
                    return EnumError(typeof(CompanyType), request.Recruiter.CompanyType);
                }
                if (request.Recruiter.LinkedInUrl is not null) profile.LinkedInUrl = request.Recruiter.LinkedInUrl.Trim();
                if (request.Recruiter.TwitterUrl is not null) profile.TwitterUrl = request.Recruiter.TwitterUrl.Trim();

                // Privacy
                if (request.Recruiter.CompanyVisibility is not null) profile.CompanyVisibility = request.Recruiter.CompanyVisibility.Trim();
                if (request.Recruiter.IsCompanyPrivate.HasValue)
                {
                    profile.IsCompanyPrivate = request.Recruiter.IsCompanyPrivate.Value;
                }

                profile.UpdatedAt = DateTime.UtcNow;
                break;
            }
        }

        await db.SaveChangesAsync();

        // ── AI Matching: notify about talent profile update ──
        if (user.UserType == UserType.Talent)
        {
            var updatedProfile = await db.TalentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (updatedProfile is not null)
            {
                var currentProfile = new Dictionary<string, object?>
                {
                    ["city"] = user.City,
                    ["country"] = user.Country,
                    ["experienceLevel"] = updatedProfile.ExperienceLevel.ToString(),
                    ["yearsOfExperience"] = updatedProfile.YearsOfExperience,
                    ["currentIndustry"] = updatedProfile.CurrentIndustry,
                    ["currentProfession"] = updatedProfile.CurrentProfession,
                    ["workMode"] = updatedProfile.WorkMode.ToString(),
                    ["skills"] = updatedProfile.Skills,
                    ["workExperience"] = updatedProfile.WorkExperience,
                    ["educationHistory"] = updatedProfile.EducationHistory,
                };
                await matchingClient.NotifyTalentUpdatedAsync(user.Id, currentProfile, previousProfile);
            }
        }

        return Results.Ok(await BuildResponseAsync(user, db));
    }

    // --------------------------------------------------------------- Helpers

    private static TalentProfile AddTalentProfile(ApplicationDbContext db, string userId)
    {
        var profile = new TalentProfile { UserId = userId };
        db.TalentProfiles.Add(profile);
        return profile;
    }

    private static RecruiterProfile AddRecruiterProfile(ApplicationDbContext db, string userId)
    {
        var profile = new RecruiterProfile { UserId = userId };
        db.RecruiterProfiles.Add(profile);
        return profile;
    }

    private static async Task<ProfileResponse> BuildResponseAsync(ApplicationUser user, ApplicationDbContext db)
    {
        TalentProfileResponse? talent = null;
        RecruiterProfileResponse? recruiter = null;
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
                    profile.PreferredSectorIds,
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
        else if (user.UserType == UserType.Recruiter)
        {
            var profile = await db.RecruiterProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile is not null)
            {
                recruiter = new RecruiterProfileResponse(
                    profile.CompanyName, profile.Industry,
                    profile.CompanySize, profile.WebsiteUrl, profile.About,
                    profile.FoundedYear, profile.Headquarters, profile.PhoneNumber, profile.Email,
                    profile.CompanyType?.ToString(), profile.LinkedInUrl, profile.TwitterUrl,
                    profile.CompanyVisibility, profile.IsCompanyPrivate);
                completion = ComputeRecruiterCompletion(profile);
            }
        }

        return new ProfileResponse(
            user.FirstName, user.MiddleName, user.LastName, user.Email ?? string.Empty, user.UserType.ToString(),
            user.AvatarUrl, user.City, user.Country, talent, recruiter, completion,
            user.PasswordHash is not null);
    }

    /// <summary>Fields the matching engine needs. Links/resume are optional extras.</summary>
    private static ProfileCompletionResponse ComputeTalentCompletion(TalentProfile profile)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.Headline)) missing.Add("headline");
        if (string.IsNullOrWhiteSpace(profile.About)) missing.Add("about");
        if (profile.ExperienceLevel is null) missing.Add("experienceLevel");
        if (profile.YearsOfExperience is null) missing.Add("yearsOfExperience");
        if (profile.DesiredRoles.Count == 0) missing.Add("desiredRoles");
        if (profile.Skills.Count == 0) missing.Add("skills");
        if (profile.DesiredJobTypes.Count == 0) missing.Add("desiredJobTypes");
        if (profile.WorkMode is null) missing.Add("workMode");
        if (profile.Availability is null) missing.Add("availability");
        if (profile.PreferredSectorIds.Count == 0) missing.Add("preferredSectors");

        const int total = 10;
        return Completion(missing, total);
    }

    /// <summary>Fields a job post depends on.</summary>
    private static ProfileCompletionResponse ComputeRecruiterCompletion(RecruiterProfile profile)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.CompanyName)) missing.Add("companyName");
        if (string.IsNullOrWhiteSpace(profile.Industry)) missing.Add("industry");
        if (string.IsNullOrWhiteSpace(profile.CompanySize)) missing.Add("companySize");
        if (string.IsNullOrWhiteSpace(profile.WebsiteUrl)) missing.Add("websiteUrl");
        if (string.IsNullOrWhiteSpace(profile.About)) missing.Add("about");
        if (profile.FoundedYear is null) missing.Add("foundedYear");
        if (string.IsNullOrWhiteSpace(profile.Headquarters)) missing.Add("headquarters");
        if (string.IsNullOrWhiteSpace(profile.PhoneNumber)) missing.Add("phoneNumber");
        if (string.IsNullOrWhiteSpace(profile.Email)) missing.Add("email");
        if (profile.CompanyType is null) missing.Add("companyType");

        const int total = 10;
        return Completion(missing, total);
    }

    private static ProfileCompletionResponse Completion(List<string> missing, int total) =>
        new(missing.Count == 0,
            (int)Math.Round((total - missing.Count) / (double)total * 100),
            missing);

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
