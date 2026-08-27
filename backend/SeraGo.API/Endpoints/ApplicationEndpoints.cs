using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Job application API — talent applies to Serago-posted jobs; recruiters
/// review and manage the applicants for their own postings.
///
/// POST   /api/applications               — talent applies to a Serago job
/// GET    /api/applications               — talent: my applications
/// GET    /api/applications/all           — recruiter: all applications across their jobs
/// GET    /api/applications/job/{jobId}   — recruiter: applications for a job
/// PATCH  /api/applications/{id}/status   — recruiter: update application status
/// GET    /api/applications/{id}          — detail (talent sees own; recruiter sees for own jobs)
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
        group.MapGet("/stats", GetRecruiterJobStatsAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        group.MapGet("/all", ListAllRecruiterApplicationsAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        group.MapGet("/job/{jobId:guid}", ListJobApplicationsAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        group.MapGet("/{id:guid}", GetApplicationAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        group.MapPatch("/{id:guid}/status", UpdateStatusAsync).RequireRateLimiting("jobs_write").WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record ApplyRequest(
        Guid JobId,
        string? CoverLetter,
        string? ResumeUrl);

    public sealed record UpdateStatusRequest(
        string Status,
        string? RecruiterNotes);

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
        string? ProfileSnapshot);

    public sealed record ApplicationListData(
        List<ApplicationResponse> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage);

    public sealed record RecruiterJobStats(
        Guid JobId,
        string JobTitle,
        string JobCompany,
        string? JobLocation,
        string JobType,
        int ViewCount,
        int PendingCount,
        int ReviewedCount,
        int InterviewCount,
        int HiredCount,
        int RejectedCount,
        int TotalApplications);

    private class JobStatsItem
    {
        public Guid JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string JobCompany { get; set; } = string.Empty;
        public string? JobLocation { get; set; }
        public string JobType { get; set; } = string.Empty;
        public int ViewCount { get; set; }
    }

    // ------------------------------------------------------------- Handlers

    /// <summary>POST /api/applications — talent applies to a Serago job.</summary>
    [Authorize(Roles = Roles.Talent)]
    private static async Task<IResult> ApplyAsync(
        ApplyRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
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
        var profile = await db.TalentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        var profileSnapshot = BuildProfileSnapshot(user, profile);

        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            UserId = user.Id,
            CoverLetter = request.CoverLetter?.Trim() ?? string.Empty,
            ResumeUrl = string.IsNullOrWhiteSpace(request.ResumeUrl)
                ? (profile?.ResumeUrl ?? null)
                : request.ResumeUrl.Trim(),
            Status = ApplicationStatus.Pending,
            AppliedAt = now,
            ProfileSnapshot = profileSnapshot,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.JobApplications.Add(application);
        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/applications/{application.Id}",
            ToResponse(application, user, job));
    }

    /// <summary>GET /api/applications — the talent's own applications.</summary>
    [Authorize(Roles = Roles.Talent)]
    private static async Task<IResult> ListMyApplicationsAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        [AsParameters] PaginationQuery pagination,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var q = db.JobApplications
            .AsNoTracking()
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.AppliedAt);

        var totalCount = await q.CountAsync(ct);
        var page = Math.Clamp(pagination.Page ?? 1, 1, 100_000);
        var pageSize = Math.Clamp(pagination.PageSize ?? 20, 1, 50);

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

    /// <summary>GET /api/applications/stats — recruiter: per-job application counts + view counts.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> GetRecruiterJobStatsAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);

        // Get the recruiter's published jobs with view counts.
        var jobs = await db.Jobs
            .AsNoTracking()
            .Where(j => (isAdmin || j.PostedByUserId == user.Id) && j.Status == JobStatus.Published && j.IsActive)
            .Select(j => new JobStatsItem
            {
                JobId = j.Id,
                JobTitle = j.Title,
                JobCompany = j.Company,
                JobLocation = j.Location,
                JobType = EnumCamel(j.JobType.ToString()),
                ViewCount = j.ViewCount,
            })
            .ToListAsync(ct);

        var jobIds = jobs.Select(j => j.JobId).ToList();

        // Get application counts per job per status.
        var statusCounts = await db.JobApplications
            .AsNoTracking()
            .Where(a => jobIds.Contains(a.JobId))
            .GroupBy(a => new { a.JobId, a.Status })
            .Select(g => new { g.Key.JobId, Status = EnumCamel(g.Key.Status.ToString()), Count = g.Count() })
            .ToListAsync(ct);

        // Build the response.
        var result = jobs.Select(j =>
        {
            var counts = statusCounts.Where(c => c.JobId == j.JobId).ToList();
            return new RecruiterJobStats(
                j.JobId,
                j.JobTitle,
                j.JobCompany,
                j.JobLocation,
                j.JobType,
                j.ViewCount,
                counts.FirstOrDefault(c => c.Status == "pending")?.Count ?? 0,
                counts.FirstOrDefault(c => c.Status == "reviewed")?.Count ?? 0,
                counts.FirstOrDefault(c => c.Status == "interview")?.Count ?? 0,
                counts.FirstOrDefault(c => c.Status == "hired")?.Count ?? 0,
                counts.FirstOrDefault(c => c.Status == "rejected")?.Count ?? 0,
                counts.Sum(c => c.Count));
        }).ToList();

        return Results.Ok(result);
    }

    /// <summary>GET /api/applications/all — recruiter: all applications across all their posted jobs.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> ListAllRecruiterApplicationsAsync(
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

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);

        IQueryable<JobApplication> q = db.JobApplications
            .AsNoTracking()
            .Where(a => isAdmin || a.Job!.PostedByUserId == user.Id);

        // Filter by status if provided.
        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<ApplicationStatus>(query.Status, ignoreCase: true, out var statusFilter))
        {
            q = q.Where(a => a.Status == statusFilter);
        }

        // Filter by specific job if provided.
        if (query.JobId.HasValue)
        {
            q = q.Where(a => a.JobId == query.JobId.Value);
        }

        // Free-text search across applicant name, email, job title, company.
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(a =>
                (a.User!.FirstName + " " + a.User.LastName).ToLower().Contains(search)
                || (a.User.Email != null && a.User.Email.ToLower().Contains(search))
                || (a.Job!.Title != null && a.Job.Title.ToLower().Contains(search))
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
            .Include(a => a.User)
            .ThenInclude(u => u!.TalentProfile)
            .Include(a => a.Job)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var response = items.Select(a => ToResponse(a, a.User!, a.Job!)).ToList();

        return Results.Ok(new ApplicationListData(
            response, totalCount, page, pageSize, totalPages, page < totalPages));
    }

    /// <summary>GET /api/applications/job/{jobId} — recruiter: all applications for a job they own.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> ListJobApplicationsAsync(
        Guid jobId,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        [AsParameters] PaginationQuery pagination,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var job = await db.Jobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        if (!isAdmin && job.PostedByUserId != user.Id)
        {
            return Results.Problem(
                "You can only view applications for jobs you posted.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var q = db.JobApplications
            .AsNoTracking()
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.AppliedAt);

        var totalCount = await q.CountAsync(ct);
        var page = Math.Clamp(pagination.Page ?? 1, 1, 100_000);
        var pageSize = Math.Clamp(pagination.PageSize ?? 20, 1, 50);

        var items = await q
            .Include(a => a.User)
            .ThenInclude(u => u!.TalentProfile)
            .Include(a => a.Job)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var response = items.Select(a =>
        {
            var applicant = a.User!;
            return ToResponse(a, applicant, a.Job!);
        }).ToList();

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
        var isRecruiterForJob = application.Job?.PostedByUserId == user.Id;

        // Talent can see their own; recruiter can see for their jobs; admin sees all.
        if (!isOwner && !isRecruiterForJob && !isAdmin)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(application, application.User!, application.Job!));
    }

    /// <summary>PATCH /api/applications/{id}/status — recruiter updates application status.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> UpdateStatusAsync(
        Guid id,
        UpdateStatusRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var application = await db.JobApplications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (application is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        if (!isAdmin && application.Job?.PostedByUserId != user.Id)
        {
            return Results.Problem(
                "You can only update applications for jobs you posted.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (!Enum.TryParse<ApplicationStatus>(request.Status, ignoreCase: true, out var newStatus))
        {
            return Results.Problem(
                $"Invalid status '{request.Status}'. Valid values: Pending, Reviewed, Interview, Hired, Rejected.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var now = DateTimeOffset.UtcNow;
        application.Status = newStatus;
        application.StatusUpdatedAt = now;
        application.UpdatedAt = now;

        if (request.RecruiterNotes is not null)
        {
            application.RecruiterNotes = request.RecruiterNotes.Trim();
        }

        await db.SaveChangesAsync(ct);

        // Reload with navigation properties for the response.
        await db.Entry(application).Reference(a => a.User).LoadAsync(ct);
        await db.Entry(application).Reference(a => a.Job).LoadAsync(ct);

        return Results.Ok(ToResponse(application, application.User!, application.Job!));
    }

    // --------------------------------------------------------------- Helpers

    /// <summary>
    /// Build a JSON snapshot of the talent's visible profile data at apply time.
    /// Respects ProfileVisibility — only includes fields the talent opted into sharing.
    /// </summary>
    private static string BuildProfileSnapshot(ApplicationUser user, TalentProfile? profile)
    {
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

        var snapshot = new Dictionary<string, object?>();

        // Identity (from ApplicationUser)
        snapshot["firstName"] = user.FirstName;
        if (IsVisible("middleName") && !string.IsNullOrWhiteSpace(user.MiddleName))
            snapshot["middleName"] = user.MiddleName;
        snapshot["lastName"] = user.LastName;
        if (IsVisible("phone") && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            snapshot["phone"] = user.PhoneNumber;
        if (IsVisible("dateOfBirth") && profile?.DateOfBirth is not null)
            snapshot["dateOfBirth"] = profile.DateOfBirth.Value.ToString("yyyy-MM-dd");
        if (IsVisible("avatar") && !string.IsNullOrWhiteSpace(user.AvatarUrl))
            snapshot["avatarUrl"] = user.AvatarUrl;
        if (IsVisible("address") && !string.IsNullOrWhiteSpace(profile?.Address))
            snapshot["address"] = profile!.Address;
        if (IsVisible("city") && !string.IsNullOrWhiteSpace(user.City))
            snapshot["city"] = user.City;
        if (IsVisible("country") && !string.IsNullOrWhiteSpace(user.Country))
            snapshot["country"] = user.Country;

        // Professional
        if (IsVisible("headline") && profile is not null && !string.IsNullOrWhiteSpace(profile.Headline))
            snapshot["headline"] = profile.Headline;
        if (IsVisible("experience") && profile is not null)
        {
            if (profile.ExperienceLevel is not null)
                snapshot["experienceLevel"] = profile.ExperienceLevel.ToString();
            if (profile.YearsOfExperience is not null)
                snapshot["yearsOfExperience"] = profile.YearsOfExperience.Value;
        }
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

        // Work experience
        if (IsVisible("experience") && profile is not null)
        {
            if (!string.IsNullOrWhiteSpace(profile.WorkExperience) && profile.WorkExperience != "[]")
                snapshot["workExperience"] = profile.WorkExperience;
        }

        // Education
        if (IsVisible("education") && profile is not null)
        {
            if (!string.IsNullOrWhiteSpace(profile.EducationLevel))
                snapshot["educationLevel"] = profile.EducationLevel;
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
        JobApplication a, ApplicationUser user, Job job) => new(
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
        a.ProfileSnapshot);

    private static string EnumCamel(string enumName) =>
        char.ToLowerInvariant(enumName[0]) + enumName[1..];

    private const string UtcDateFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static string FormatDate(DateTimeOffset value) =>
        value.ToUniversalTime().ToString(UtcDateFormat, CultureInfo.InvariantCulture);

    private static string? FormatDate(DateTimeOffset? value) =>
        value.HasValue ? FormatDate(value.Value) : null;

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed class PaginationQuery
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public sealed class ApplicationListQuery
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }

        /// <summary>Filter by status: pending, reviewed, accepted, rejected.</summary>
        public string? Status { get; set; }

        /// <summary>Filter by a specific job id the recruiter posted.</summary>
        public Guid? JobId { get; set; }

        /// <summary>Free-text search across applicant name, email, job title, company.</summary>
        public string? Search { get; set; }

        /// <summary>Sort order: "newest" (default) or "oldest".</summary>
        public string? Sort { get; set; }
    }
}
