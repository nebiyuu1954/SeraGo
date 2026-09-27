using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Services;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Job application API — talents apply to Serago-posted jobs and track their
/// own applications.
///
/// POST   /api/applications               — talent applies to a Serago job
/// GET    /api/applications               — talent: my applications
/// GET    /api/applications/{id}          — detail (the talent's own; admins see all)
///
/// Only Serago-posted jobs (SourceName null or "SeraGo") accept applications.
/// External scraper jobs redirect the applicant to the source website.
/// </summary>
public static class ApplicationEndpoints
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/applications").WithTags("Applications");

        group.MapPost("/", ApplyAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapGet("/", ListMyApplicationsAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        group.MapGet("/{id:guid}", GetApplicationAsync).RequireRateLimiting("jobs_read").WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record ApplyRequest(
        Guid JobId,
        string? CoverLetter,
        string? ResumeUrl,
        bool? ShareProfile);

    public sealed record ApplicationResponse(
        Guid Id,
        Guid JobId,
        string JobTitle,
        string JobCompany,
        string? JobLocation,
        string? JobSourceName,
        string UserId,
        string ApplicantName,
        string ApplicantEmail,
        string? ApplicantHeadline,
        string? ApplicantAvatarUrl,
        string? CoverLetter,
        string? ResumeUrl,
        string Status,
        string AppliedAt,
        string? StatusUpdatedAt,
        bool ProfileShared,
        string? ProfileSnapshot,
        string? JobSalary,
        string? JobDeadline,
        string? CompanyLogoUrl,
        // AI match score (0-100) — populated when the AI service scored it.
        int? MatchScore,
        List<string>? MatchedSkills,
        List<string>? MissingSkills);

    public sealed record ApplicationListData(
        List<ApplicationResponse> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage);

    // ------------------------------------------------------------- Handlers

    /// <summary>POST /api/applications — talent applies to a Serago job.</summary>
    [Authorize(Roles = Roles.Talent)]
    private static async Task<IResult> ApplyAsync(
        ApplyRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        NotificationService notificationService,
        MatchingClient matchingClient,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var job = await db.Jobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == request.JobId, ct);
        if (job is null)
        {
            return Results.NotFound();
        }

        // Only Serago-posted jobs accept applications.
        var isSeragoJob = string.IsNullOrEmpty(job.SourceName)
            || job.SourceName.Equals("SeraGo", StringComparison.OrdinalIgnoreCase);
        if (!isSeragoJob)
        {
            return Results.Problem(
                "This job is from an external website. Please apply directly on the original platform.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Job must be published and active.
        if (job.Status != JobStatus.Published || !job.IsActive)
        {
            return Results.Problem(
                "This job is no longer accepting applications.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // One application per talent per job.
        var existing = await db.JobApplications
            .AnyAsync(a => a.JobId == request.JobId && a.UserId == user.Id, ct);
        if (existing)
        {
            return Results.Problem(
                "You have already applied to this job.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var now = DateTimeOffset.UtcNow;

        // Snapshot the talent's visible profile data at apply time.
        var shareProfile = request.ShareProfile ?? false;
        var profile = await db.TalentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        var profileSnapshot = BuildProfileSnapshot(user, profile, shareProfile);

        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            UserId = user.Id,
            CoverLetter = request.CoverLetter?.Trim() ?? string.Empty,
            ResumeUrl = string.IsNullOrWhiteSpace(request.ResumeUrl)
                ? (shareProfile ? null : (profile?.ResumeUrl ?? null))
                : request.ResumeUrl.Trim(),
            Status = ApplicationStatus.Pending,
            AppliedAt = now,
            ProfileShared = shareProfile,
            ProfileSnapshot = profileSnapshot,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.JobApplications.Add(application);
        await db.SaveChangesAsync(ct);

        // Notify the job poster (an admin) that someone applied
        if (job.PostedByUserId is not null)
        {
            var talentName = user.FirstName is not null && user.LastName is not null
                ? $"{user.FirstName} {user.LastName}"
                : user.UserName ?? "A talent";

            await notificationService.CreateAsync(new NotificationService.CreateNotificationRequest(
                UserId: job.PostedByUserId,
                Type: NotificationType.ApplicationReceived,
                Title: "New application received",
                Body: $"{talentName} applied to {job.Title}",
                Data: $"{{\"job_id\":\"{job.Id}\",\"application_id\":\"{application.Id}\",\"actor_name\":\"{talentName}\"}}",
                SkipEmail: false,
                SkipTelegram: true
            ));
        }

        // ── AI Matching: store application for deferred scoring ──
        var talentProfileDict = new Dictionary<string, object?>
        {
            ["userId"] = user.Id,
            ["headline"] = profile?.Headline,
            ["about"] = profile?.About,
            ["skills"] = profile?.Skills ?? [],
            ["experienceLevel"] = profile?.ExperienceLevel.ToString(),
            ["yearsOfExperience"] = profile?.YearsOfExperience,
            ["currentIndustry"] = profile?.CurrentIndustry,
            ["currentProfession"] = profile?.CurrentProfession,
            ["workMode"] = profile?.WorkMode.ToString(),
            ["workExperience"] = profile?.WorkExperience,
            ["educationHistory"] = profile?.EducationHistory,
        };
        var jobDict = new Dictionary<string, object?>
        {
            ["jobId"] = job.Id.ToString(),
            ["title"] = job.Title,
            ["description"] = job.Description,
            ["company"] = job.Company,
            ["sectorId"] = job.SectorId?.ToString(),
            ["sectorName"] = job.SectorName,
            ["experienceLevel"] = job.ExperienceLevel.ToString(),
            ["jobType"] = job.JobType.ToString(),
            ["workMode"] = job.WorkMode.ToString(),
            ["skills"] = job.Skills,
        };
        await matchingClient.NotifyApplicationCreatedAsync(
            application.Id, user.Id, job.Id, talentProfileDict, jobDict, ct);

        return Results.Created(
            $"/api/applications/{application.Id}",
            ToResponse(application, user, job));
    }

    /// <summary>
    /// Annotate application responses with the stored AI match score (0-100).
    /// Best-effort — when the AI service is unreachable or hasn't scored an
    /// application yet, the responses are returned unchanged.
    /// </summary>
    private static async Task<List<ApplicationResponse>> AnnotateApplicationScoresAsync(
        List<ApplicationResponse> responses, MatchingClient matchingClient, CancellationToken ct)
    {
        if (responses.Count == 0) return responses;

        var scores = await matchingClient.GetApplicationScoresAsync(
            responses.Select(r => r.Id).ToList(), ct);
        if (scores.Count == 0) return responses;

        return responses.Select(r => scores.TryGetValue(r.Id, out var s) && s.Score is not null
            ? r with
            {
                MatchScore = (int?)Math.Round(s.Score.Value),
                MatchedSkills = s.Matched,
                MissingSkills = s.Missing,
            }
            : r).ToList();
    }

    /// <summary>GET /api/applications — the talent's own applications.</summary>
    [Authorize(Roles = Roles.Talent)]
    private static async Task<IResult> ListMyApplicationsAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        [AsParameters] ApplicationListQuery query,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        IQueryable<JobApplication> q = db.JobApplications
            .AsNoTracking()
            .Where(a => a.UserId == user.Id);

        // Filter by status if provided.
        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<ApplicationStatus>(query.Status, ignoreCase: true, out var statusFilter))
        {
            q = q.Where(a => a.Status == statusFilter);
        }

        // Free-text search across job title and company.
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(a =>
                (a.Job!.Title != null && a.Job.Title.ToLower().Contains(search))
                || (a.Job.Company != null && a.Job.Company.ToLower().Contains(search))
            );
        }

        // Sort.
        q = query.Sort?.ToLowerInvariant() switch
        {
            "oldest" => q.OrderBy(a => a.AppliedAt),
            _ => q.OrderByDescending(a => a.AppliedAt),
        };

        var totalCount = await q.CountAsync(ct);
        var page = Math.Clamp(query.Page ?? 1, 1, 100_000);
        var pageSize = Math.Clamp(query.PageSize ?? 20, 1, 50);

        var items = await q
            .Include(a => a.Job)
            .Include(a => a.User)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var response = items.Select(a => ToResponse(a, a.User!, a.Job!)).ToList();

        return Results.Ok(new ApplicationListData(
            response, totalCount, page, pageSize, totalPages, page < totalPages));
    }

    /// <summary>GET /api/applications/{id} — detail view.</summary>
    [Authorize]
    private static async Task<IResult> GetApplicationAsync(
        Guid id,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        MatchingClient matchingClient,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var application = await db.JobApplications
            .AsNoTracking()
            .Include(a => a.Job)
            .Include(a => a.User)
            .ThenInclude(u => u!.TalentProfile)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (application is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        var isOwner = application.UserId == user.Id;

        // A talent can see their own application; admins see all.
        if (!isOwner && !isAdmin)
        {
            return Results.NotFound();
        }

        var response = await AnnotateApplicationScoresAsync(
            [ToResponse(application, application.User!, application.Job!)],
            matchingClient, ct);

        return Results.Ok(response[0]);
    }

    // --------------------------------------------------------------- Helpers

    private static string BuildProfileSnapshot(ApplicationUser user, TalentProfile? profile, bool shareProfile)
    {
        var snapshot = new Dictionary<string, object?>();

        // ── Mandatory fields — always included ──
        snapshot["firstName"] = user.FirstName;
        snapshot["lastName"] = user.LastName;
        if (!string.IsNullOrWhiteSpace(user.City))
            snapshot["city"] = user.City;
        if (!string.IsNullOrWhiteSpace(user.Country))
            snapshot["country"] = user.Country;
        if (profile is not null)
        {
            if (profile.ExperienceLevel is not null)
                snapshot["experienceLevel"] = profile.ExperienceLevel.ToString();
            if (profile.YearsOfExperience is not null)
                snapshot["yearsOfExperience"] = profile.YearsOfExperience.Value;
            if (!string.IsNullOrWhiteSpace(profile.EducationLevel))
                snapshot["educationLevel"] = profile.EducationLevel;
        }

        // ── Full profile — only when talent explicitly shared it ──
        if (!shareProfile)
            return System.Text.Json.JsonSerializer.Serialize(snapshot);

        // Parse visibility toggles — default to true for any missing key.
        Dictionary<string, bool> visibility;
        try
        {
            visibility = string.IsNullOrWhiteSpace(profile?.ProfileVisibility)
                ? new Dictionary<string, bool>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(profile!.ProfileVisibility)
                  ?? new Dictionary<string, bool>();
        }
        catch
        {
            visibility = new Dictionary<string, bool>();
        }

        bool IsVisible(string key) => !visibility.TryGetValue(key, out var v) || v;

        // Identity extras
        if (IsVisible("middleName") && !string.IsNullOrWhiteSpace(user.MiddleName))
            snapshot["middleName"] = user.MiddleName;
        if (IsVisible("phone") && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            snapshot["phone"] = user.PhoneNumber;
        if (IsVisible("dateOfBirth") && profile?.DateOfBirth is not null)
            snapshot["dateOfBirth"] = profile.DateOfBirth.Value.ToString("yyyy-MM-dd");
        if (IsVisible("avatar") && !string.IsNullOrWhiteSpace(user.AvatarUrl))
            snapshot["avatarUrl"] = user.AvatarUrl;
        if (IsVisible("address") && !string.IsNullOrWhiteSpace(profile?.Address))
            snapshot["address"] = profile!.Address;

        // Professional
        if (IsVisible("about") && profile is not null && !string.IsNullOrWhiteSpace(profile.About))
            snapshot["about"] = profile.About;
        if (IsVisible("skills") && profile is not null && profile.Skills.Count > 0)
        {
            // Per-skill visibility: only include skills the talent opted into.
            Dictionary<string, bool> skillVis;
            try
            {
                skillVis = string.IsNullOrWhiteSpace(profile.SkillVisibility)
                    ? new Dictionary<string, bool>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(profile.SkillVisibility)
                      ?? new Dictionary<string, bool>();
            }
            catch { skillVis = new Dictionary<string, bool>(); }

            var visibleSkills = profile.Skills
                .Where(s => !skillVis.TryGetValue(s, out var v) || v)
                .ToList();
            if (visibleSkills.Count > 0)
                snapshot["skills"] = visibleSkills;
        }
        if (IsVisible("currentIndustry") && profile is not null && !string.IsNullOrWhiteSpace(profile.CurrentIndustry))
            snapshot["currentIndustry"] = profile.CurrentIndustry;
        if (IsVisible("currentProfession") && profile is not null && !string.IsNullOrWhiteSpace(profile.CurrentProfession))
            snapshot["currentProfession"] = profile.CurrentProfession;
        if (IsVisible("workMode") && profile is not null && profile.WorkMode is not null)
            snapshot["workMode"] = profile.WorkMode.ToString();
        if (IsVisible("availability") && profile is not null && profile.Availability is not null)
            snapshot["availability"] = profile.Availability.ToString();
        if (IsVisible("desiredRoles") && profile is not null && profile.DesiredRoles.Count > 0)
            snapshot["desiredRoles"] = profile.DesiredRoles;

        // Work experience
        if (IsVisible("experience") && profile is not null)
        {
            if (!string.IsNullOrWhiteSpace(profile.WorkExperience) && profile.WorkExperience != "[]")
                snapshot["workExperience"] = profile.WorkExperience;
        }

        // Education history
        if (IsVisible("education") && profile is not null)
        {
            if (!string.IsNullOrWhiteSpace(profile.EducationHistory) && profile.EducationHistory != "[]")
                snapshot["educationHistory"] = profile.EducationHistory;
        }

        // Links
        if (IsVisible("linkedin") && profile is not null && !string.IsNullOrWhiteSpace(profile.LinkedInUrl))
            snapshot["linkedinUrl"] = profile.LinkedInUrl;
        if (IsVisible("github") && profile is not null && !string.IsNullOrWhiteSpace(profile.GitHubUrl))
            snapshot["githubUrl"] = profile.GitHubUrl;
        if (IsVisible("portfolio") && profile is not null && !string.IsNullOrWhiteSpace(profile.PortfolioUrl))
            snapshot["portfolioUrl"] = profile.PortfolioUrl;
        if (IsVisible("resume") && profile is not null && !string.IsNullOrWhiteSpace(profile.ResumeUrl))
            snapshot["resumeUrl"] = profile.ResumeUrl;

        // Preferred locations
        if (IsVisible("preferredLocations") && profile is not null && !string.IsNullOrWhiteSpace(profile.PreferredLocations) && profile.PreferredLocations != "[]")
            snapshot["preferredLocations"] = profile.PreferredLocations;

        return System.Text.Json.JsonSerializer.Serialize(snapshot);
    }

    private static ApplicationResponse ToResponse(
        JobApplication a, ApplicationUser user, Job job)
    {
        // Company logo: prefer the job's own logo, fall back to the poster's avatar.
        var companyLogo = NullIfBlank(job.CompanyLogoUrl)
                          ?? NullIfBlank(job.PostedBy?.AvatarUrl);

        return new(
        a.Id,
        a.JobId,
        job.Title,
        job.Company,
        job.Location,
        job.SourceName,
        a.UserId,
        $"{user.FirstName} {user.LastName}".Trim(),
        user.Email ?? string.Empty,
        user.TalentProfile?.Headline,
        NullIfBlank(user.AvatarUrl),
        NullIfBlank(a.CoverLetter),
        a.ResumeUrl,
        EnumCamel(a.Status.ToString()),
        FormatDate(a.AppliedAt),
        FormatDate(a.StatusUpdatedAt),
        a.ProfileShared,
        a.ProfileSnapshot,
        NullIfBlank(job.Salary),
        FormatDate(job.Deadline),
        companyLogo,
        null,   // MatchScore — annotated after listing where applicable
        null,   // MatchedSkills
        null);  // MissingSkills
    }

    private static string EnumCamel(string enumName) =>
        char.ToLowerInvariant(enumName[0]) + enumName[1..];

    private const string UtcDateFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static string FormatDate(DateTimeOffset value) =>
        value.ToUniversalTime().ToString(UtcDateFormat, CultureInfo.InvariantCulture);

    private static string? FormatDate(DateTimeOffset? value) =>
        value.HasValue ? FormatDate(value.Value) : null;

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed class ApplicationListQuery
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }

        /// <summary>Filter by status: pending, reviewed, accepted, rejected.</summary>
        public string? Status { get; set; }

        /// <summary>Free-text search across job title, company.</summary>
        public string? Search { get; set; }

        /// <summary>Sort order: "newest" (default) or "oldest".</summary>
        public string? Sort { get; set; }
    }
}
