using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Services;
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
        // Recruiter-initiated: score the job's applicants now ("Run AI matching").
        group.MapPost("/job/{jobId:guid}/match", MatchApplicationsAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapGet("/{id:guid}", GetApplicationAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        group.MapPatch("/{id:guid}/status", UpdateStatusAsync).RequireRateLimiting("jobs_write").WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record ApplyRequest(
        Guid JobId,
        string? CoverLetter,
        string? ResumeUrl,
        bool? ShareProfile);

    public sealed record UpdateApplicationStatusRequest(
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

    /// <summary>The envelope's data for POST /api/applications/job/{jobId}/match.</summary>
    public sealed record ApplicationsMatchResponse(
        int Scored, int Cached, int Failed, int Total, string? Message);

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
            .Include(j => j.PostedBy)
                .ThenInclude(u => u!.RecruiterProfile)
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

        // Notify the recruiter that someone applied
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

        // Load job + user + the job poster's RecruiterProfile (for company-privacy check).
        var items = await q
            .Include(a => a.Job)
                .ThenInclude(j => j!.PostedBy)
                    .ThenInclude(u => u!.RecruiterProfile)
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
        MatchingClient matchingClient,
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

        var response = await AnnotateApplicationScoresAsync(
            items.Select(a => ToResponse(a, a.User!, a.Job!)).ToList(),
            matchingClient, ct);

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
        MatchingClient matchingClient,
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

        var response = await AnnotateApplicationScoresAsync(
            items.Select(a =>
            {
                var applicant = a.User!;
                return ToResponse(a, applicant, a.Job!);
            }).ToList(),
            matchingClient, ct);

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
                .ThenInclude(j => j!.PostedBy)
                    .ThenInclude(u => u!.RecruiterProfile)
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

        var response = await AnnotateApplicationScoresAsync(
            [ToResponse(application, application.User!, application.Job!)],
            matchingClient, ct);

        return Results.Ok(response[0]);
    }

    /// <summary>PATCH /api/applications/{id}/status — recruiter updates application status.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> UpdateStatusAsync(
        Guid id,
        UpdateApplicationStatusRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        NotificationService notificationService,
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

        // Notify the talent that their application status changed
        var talentName = application.User?.FirstName is not null && application.User?.LastName is not null
            ? $"{application.User.FirstName} {application.User.LastName}"
            : application.User?.UserName ?? "You";
        var jobTitle = application.Job?.Title ?? "the job";
        var statusLabel = newStatus switch
        {
            ApplicationStatus.Reviewed => "reviewed",
            ApplicationStatus.Interview => "moved to interview",
            ApplicationStatus.Hired => "accepted",
            ApplicationStatus.Rejected => "not selected",
            _ => newStatus.ToString().ToLower()
        };

        await notificationService.CreateAsync(new NotificationService.CreateNotificationRequest(
            UserId: application.UserId,
            Type: NotificationType.ApplicationStatusChanged,
            Title: "Application status updated",
            Body: $"Your application for {jobTitle} has been {statusLabel}",
            Data: $"{{\"job_id\":\"{application.JobId}\",\"application_id\":\"{application.Id}\",\"new_status\":\"{newStatus}\"}}",
            SkipEmail: false,
            SkipTelegram: true
        ));

        // Reload with navigation properties for the response.
        await db.Entry(application).Reference(a => a.User).LoadAsync(ct);
        await db.Entry(application).Reference(a => a.Job).LoadAsync(ct);

        return Results.Ok(ToResponse(application, application.User!, application.Job!));
    }

    // --------------------------------------------------------------- Helpers

    /// <summary>
    /// Build a JSON snapshot of the talent's profile data at apply time.
    /// When shareProfile is true, includes all visible fields (respecting ProfileVisibility).
    /// When false (resume-only), only mandatory fields are included: name, email,
    /// city, country, experience level, years of experience, and highest education level.
    /// </summary>
    /// <summary>
    /// POST /api/applications/job/{jobId}/match — the recruiter's "Run AI
    /// matching" button on the job's applicants. Scores every application of
    /// the job (from each applicant's apply-time profile snapshot) right now
    /// via the AI service; the frontend then re-fetches the list, whose items
    /// are annotated with the stored scores.
    /// </summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> MatchApplicationsAsync(
        Guid jobId,
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

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
        {
            return Results.NotFound();
        }
        if (!isAdmin && job.PostedByUserId != user.Id)
        {
            // Don't leak other recruiters' jobs/applicants.
            return Results.NotFound();
        }

        var applications = await db.JobApplications.AsNoTracking()
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.AppliedAt)
            .Take(200)
            .ToListAsync(ct);

        if (applications.Count == 0)
        {
            return Results.Ok(new ApplicationsMatchResponse(0, 0, 0, 0,
                "No one has applied to this job yet."));
        }

        var entries = applications.Select(a => BuildApplicationMatchEntry(a, job)).ToList();
        var result = await matchingClient.RescoreApplicationsAsync(entries, ct);

        if (!result.Succeeded)
        {
            return Results.Ok(new ApplicationsMatchResponse(0, 0, 0, entries.Count,
                "The AI matching service is not reachable right now — try again in a moment."));
        }
        if (result.Scored == 0 && result.Cached == 0)
        {
            return Results.Ok(new ApplicationsMatchResponse(0, 0, result.Failed, entries.Count,
                "None could be matched — applicants who didn't share enough of their profile when applying can't be scored."));
        }
        if (result.Scored == 0 && result.Cached > 0)
        {
            return Results.Ok(new ApplicationsMatchResponse(0, result.Cached, result.Failed, entries.Count,
                "Matches are already up to date."));
        }

        return Results.Ok(new ApplicationsMatchResponse(
            result.Scored, result.Cached, result.Failed, entries.Count, null));
    }

    /// <summary>
    /// Builds one AI rescore payload entry from an application's apply-time
    /// profile snapshot (<see cref="JobApplication.ProfileSnapshot"/>). The
    /// snapshot is the source of truth — what the talent shared when they
    /// applied — even if they later changed their profile. Applications
    /// without enough shared data simply produce an entry the AI reports as
    /// unscorable.
    /// </summary>
    private static Dictionary<string, object?> BuildApplicationMatchEntry(
        JobApplication app, Job job)
    {
        JsonElement root;
        if (!string.IsNullOrWhiteSpace(app.ProfileSnapshot))
        {
            try
            {
                using var doc = JsonDocument.Parse(app.ProfileSnapshot);
                root = doc.RootElement.Clone();
            }
            catch
            {
                root = default;
            }
        }
        else
        {
            root = default;
        }

        string Str(string key)
        {
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty(key, out var e)
                || e.ValueKind != JsonValueKind.String)
            {
                return string.Empty;
            }
            return e.GetString() ?? string.Empty;
        }

        var talent = new Dictionary<string, object?>
        {
            ["userId"] = app.UserId,
            ["headline"] = null,
            ["about"] = NullIfBlank(Str("about")),
            ["skills"] = ReadStringList(root, "skills"),
            ["experienceLevel"] = NullIfBlank(Str("experienceLevel")),
            ["yearsOfExperience"] = ReadNullableInt(root, "yearsOfExperience"),
            ["currentIndustry"] = NullIfBlank(Str("currentIndustry")),
            ["currentProfession"] = NullIfBlank(Str("currentProfession")),
            ["workMode"] = NullIfBlank(Str("workMode")),
            ["desiredRoles"] = ReadStringList(root, "desiredRoles"),
            ["desiredJobTypes"] = new List<string>(),
            ["preferredLocations"] = ReadStringList(root, "preferredLocations"),
            ["workExperience"] = NullIfBlank(Str("workExperience")),
            ["educationHistory"] = NullIfBlank(Str("educationHistory")),
            ["preferredSectorIds"] = new List<string>(),
        };

        return new Dictionary<string, object?>
        {
            ["applicationId"] = app.Id.ToString(),
            ["talentProfile"] = talent,
            ["job"] = BuildJobMatchingPayload(job),
        };
    }

    private static List<string> ReadStringList(JsonElement root, string key)
    {
        var result = new List<string>();
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(key, out var e))
        {
            return result;
        }

        void AddStrings(JsonElement arr)
        {
            foreach (var item in arr.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(item.GetString()))
                {
                    result.Add(item.GetString()!);
                }
            }
        }

        if (e.ValueKind == JsonValueKind.Array)
        {
            AddStrings(e);
        }
        else if (e.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(e.GetString()))
        {
            // Some snapshot lists (e.g. preferredLocations) are stored as a
            // JSON string — unwrap them.
            try
            {
                using var nested = JsonDocument.Parse(e.GetString()!);
                if (nested.RootElement.ValueKind == JsonValueKind.Array)
                {
                    AddStrings(nested.RootElement);
                }
            }
            catch
            {
                // Not valid JSON — ignore.
            }
        }

        return result;
    }

    private static int? ReadNullableInt(JsonElement root, string key)
    {
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(key, out var e)
            && e.ValueKind == JsonValueKind.Number
            && e.TryGetInt32(out var value))
        {
            return value;
        }
        return null;
    }

    /// <summary>The job fields the AI service scores against (same shape as
    /// the webhook payloads).</summary>
    private static Dictionary<string, object?> BuildJobMatchingPayload(Job job) => new()
    {
        ["jobId"] = job.Id.ToString(),
        ["title"] = job.Title,
        ["description"] = job.Description,
        ["company"] = job.Company,
        ["location"] = job.Location,
        ["sectorId"] = job.SectorId?.ToString(),
        ["sectorName"] = job.SectorName,
        ["experienceLevel"] = job.ExperienceLevel,
        ["jobType"] = job.JobType.ToString(),
        ["workMode"] = job.WorkMode.ToString(),
        ["skills"] = job.Skills,
        ["experienceMinYears"] = job.ExperienceMinYears,
        ["experienceMaxYears"] = job.ExperienceMaxYears,
    };

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
        // Check if the job poster's company is private.
        var isCompanyPrivate = job.PostedBy?.RecruiterProfile?.IsCompanyPrivate == true;

        // Company logo: prefer job's own logo, fall back to the recruiter's avatar.
        var companyLogo = NullIfBlank(job.CompanyLogoUrl)
                          ?? NullIfBlank(job.PostedBy?.AvatarUrl);

        return new(
        a.Id,
        a.JobId,
        job.Title,
        isCompanyPrivate ? "Confidential Company" : job.Company,
        isCompanyPrivate ? null : job.Location,
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
        isCompanyPrivate ? null : companyLogo,
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
