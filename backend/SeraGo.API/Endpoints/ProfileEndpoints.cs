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
        MatchingClient matchingClient)
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

        // The AI service is optional and an unreadable resume is a normal
        // outcome, not an error the user must fix: unreachable ⇒ success:false.
        // A scanned (image-only) PDF arrives the same way with charsExtracted 0.
        return Results.Ok(result ?? new MatchingClient.ResumeParseResult { Success = false });
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
