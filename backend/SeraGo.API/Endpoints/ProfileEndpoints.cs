using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        string? WorkMode, string? Availability,
        string ResumeUrl, string LinkedInUrl, string GitHubUrl, string PortfolioUrl);

    public sealed record RecruiterProfileResponse(
        string CompanyName, string CompanyLogoUrl, string Industry, string CompanySize,
        string WebsiteUrl, string About);

    public sealed record ProfileResponse(
        string FirstName, string LastName, string Email, string Role, string AvatarUrl,
        string City, string Country, TalentProfileResponse? Talent,
        RecruiterProfileResponse? Recruiter, ProfileCompletionResponse? Completion);

    public sealed class UpdateProfileRequest
    {
        // Common — shared by every role.
        public string? FirstName { get; set; }
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
        public string? WorkMode { get; set; }
        public string? Availability { get; set; }
        public string? ResumeUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? GitHubUrl { get; set; }
        public string? PortfolioUrl { get; set; }
    }

    public sealed class RecruiterProfileUpdate
    {
        public string? CompanyName { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? Industry { get; set; }
        public string? CompanySize { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? About { get; set; }
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

                profile.Headline = request.Talent.Headline?.Trim() ?? string.Empty;
                profile.About = request.Talent.About?.Trim() ?? string.Empty;
                profile.ExperienceLevel = experienceLevel;
                profile.YearsOfExperience = request.Talent.YearsOfExperience;
                profile.DesiredRoles = request.Talent.DesiredRoles ?? [];
                profile.Skills = request.Talent.Skills ?? [];
                profile.DesiredJobTypes = NormalizeJobTypes(request.Talent.DesiredJobTypes, out var jobTypeError);
                if (jobTypeError is not null)
                {
                    return Results.Problem(jobTypeError, statusCode: StatusCodes.Status400BadRequest);
                }
                profile.WorkMode = workMode;
                profile.Availability = availability;
                profile.ResumeUrl = request.Talent.ResumeUrl?.Trim() ?? string.Empty;
                profile.LinkedInUrl = request.Talent.LinkedInUrl?.Trim() ?? string.Empty;
                profile.GitHubUrl = request.Talent.GitHubUrl?.Trim() ?? string.Empty;
                profile.PortfolioUrl = request.Talent.PortfolioUrl?.Trim() ?? string.Empty;
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
                profile.CompanyLogoUrl = request.Recruiter.CompanyLogoUrl?.Trim() ?? string.Empty;
                profile.Industry = request.Recruiter.Industry?.Trim() ?? string.Empty;
                profile.CompanySize = request.Recruiter.CompanySize?.Trim() ?? string.Empty;
                profile.WebsiteUrl = request.Recruiter.WebsiteUrl?.Trim() ?? string.Empty;
                profile.About = request.Recruiter.About?.Trim() ?? string.Empty;
                profile.UpdatedAt = DateTime.UtcNow;
                break;
            }
        }

        await db.SaveChangesAsync();
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
                    profile.ResumeUrl, profile.LinkedInUrl, profile.GitHubUrl, profile.PortfolioUrl);
                completion = ComputeTalentCompletion(profile);
            }
        }
        else if (user.UserType == UserType.Recruiter)
        {
            var profile = await db.RecruiterProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile is not null)
            {
                recruiter = new RecruiterProfileResponse(
                    profile.CompanyName, profile.CompanyLogoUrl, profile.Industry,
                    profile.CompanySize, profile.WebsiteUrl, profile.About);
                completion = ComputeRecruiterCompletion(profile);
            }
        }

        return new ProfileResponse(
            user.FirstName, user.LastName, user.Email ?? string.Empty, user.UserType.ToString(),
            user.AvatarUrl, user.City, user.Country, talent, recruiter, completion);
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

        const int total = 9;
        return Completion(missing, total);
    }

    /// <summary>Fields a job post depends on. Logo is optional.</summary>
    private static ProfileCompletionResponse ComputeRecruiterCompletion(RecruiterProfile profile)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.CompanyName)) missing.Add("companyName");
        if (string.IsNullOrWhiteSpace(profile.Industry)) missing.Add("industry");
        if (string.IsNullOrWhiteSpace(profile.CompanySize)) missing.Add("companySize");
        if (string.IsNullOrWhiteSpace(profile.WebsiteUrl)) missing.Add("websiteUrl");
        if (string.IsNullOrWhiteSpace(profile.About)) missing.Add("about");

        const int total = 5;
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
