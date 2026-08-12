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
/// Job browse + posting API for SeraGo's own jobs table (no scraper
/// involvement).
///
/// GET    /api/jobs                     — list with sorting, filtering, search, pagination
/// GET    /api/jobs/{id}                — detail
/// POST   /api/jobs                     — create (Recruiter, Admin). saveAsDraft=true keeps a
///                                        hidden draft; saveAsDraft=false submits for admin approval.
/// PUT    /api/jobs/{id}                — full update. Recruiters edit their own non-published
///                                        jobs (a rejected job resets to draft on edit); admins edit anything.
/// DELETE /api/jobs/{id}                — soft delete (is_active=false). Recruiters delete their
///                                        own jobs; admins pass ?hard=true for a permanent delete.
/// PATCH  /api/jobs/{id}/submit         — owner: draft/rejected → pending approval
/// PATCH  /api/jobs/{id}/approve        — admin: pending → published (live)
/// PATCH  /api/jobs/{id}/reject         — admin: pending → rejected (+ reason)
/// PATCH  /api/jobs/{id}/restore        — admin: unhide a soft-deleted job
///
/// Roles: Talent can read only. Recruiter can create/read/update/soft-delete
/// their own postings (admin approval gates going live). Admin has full rights.
///
/// Throttling: reads and writes opt into the "jobs_read" / "jobs_write" rate
/// limit policies (fixed window per IP, limits in the RateLimiting config).
///
/// Response format: all responses pass through the standard envelope middleware
/// (<c>{ responseStatus, messageCode, message, data }</c> — the list's data is
/// <c>{ items, pagination }</c>). Dates are UTC ISO-8601
/// ("yyyy-MM-dd'T'HH:mm:ss'Z'"), enums are lowerCamelCase ("fullTime", "draft").
/// </summary>
public static class JobEndpoints
{
    private const int MaxPageSize = 50;

    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/jobs").WithTags("Jobs");

        group.MapGet("/", ListJobsAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        group.MapGet("/{id:guid}", GetJobAsync).RequireRateLimiting("jobs_read").WithOpenApi();

        group.MapPost("/", CreateJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapPut("/{id:guid}", UpdateJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapDelete("/{id:guid}", DeleteJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();

        group.MapPatch("/{id:guid}/submit", SubmitJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapPatch("/{id:guid}/approve", ApproveJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapPatch("/{id:guid}/reject", RejectJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapPatch("/{id:guid}/restore", RestoreJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed class JobListQuery
    {
        // Search + filters — every query parameter is OPTIONAL, so a bare
        // GET /api/jobs always binds.
        public string? Q { get; set; }          // free-text: title / company / description
        public string? JobType { get; set; }    // "FULL_TIME" or "FullTime"
        public string? Location { get; set; }   // substring of the location field
        public string? Sort { get; set; }       // newest | oldest | title_asc | title_desc | deadline

        // Pagination
        public int? Page { get; set; }          // default 1
        public int? PageSize { get; set; }      // default 10

        // Scope (recruiter/admin) — nullable so the binder never treats a
        // missing parameter as required.
        public bool? Mine { get; set; }            // my own jobs (any status) — owner only
        public string? Status { get; set; }        // status filter — admin (or with Mine)
        public bool? IncludeInactive { get; set; } // include hidden jobs — admin only
    }

    public sealed class JobWriteRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Company { get; set; }
        public string? Location { get; set; }
        public string? JobType { get; set; }
        public string? Url { get; set; }
        public string? Salary { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public DateTimeOffset? Deadline { get; set; }

        /// <summary>true → keep as a hidden draft (default). false → submit for review (or publish directly for admins).</summary>
        public bool SaveAsDraft { get; set; } = true;
    }

    public sealed record RejectJobRequest(string? Reason);

    public sealed record JobResponse(
        Guid Id,
        string Title,
        string Description,
        string Company,
        string Location,
        string JobType,          // lowerCamel enum name, e.g. "fullTime"
        string Url,
        string Salary,
        string? PublishedAt,     // UTC ISO-8601, e.g. "2026-09-15T14:00:00Z"
        string? Deadline,
        string Status,           // lowerCamel enum name, e.g. "draft"
        bool IsActive,
        bool IsOwner,
        string? RejectionReason, // null when empty
        string CreatedAt,
        string UpdatedAt);

    public sealed record PaginationResponse(
        int Page, int PageSize, int TotalCount, int TotalPages, bool HasNextPage);

    /// <summary>The envelope's data for GET /api/jobs.</summary>
    public sealed record JobListData(List<JobResponse> Items, PaginationResponse Pagination);

    // ------------------------------------------------------------- Handlers

    /// <summary>GET /api/jobs — authenticated read with sorting, filtering, search and pagination.</summary>
    [Authorize]
    private static async Task<IResult> ListJobsAsync(
        [AsParameters] JobListQuery query,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);

        var q = db.Jobs.AsNoTracking().AsQueryable();

        // Scope + visibility.
        if (query.Mine == true)
        {
            q = q.Where(j => j.PostedByUserId == user.Id);

            // Owners may narrow their own jobs by status (?mine=true&status=Draft).
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                if (!TryParseJobStatus(query.Status, out var status))
                {
                    return EnumError(typeof(JobStatus), query.Status);
                }
                q = q.Where(j => j.Status == status);
            }
        }
        else if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!isAdmin)
            {
                return Results.Problem(
                    "Only admins may filter by job status.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
            if (!TryParseJobStatus(query.Status, out var status))
            {
                return EnumError(typeof(JobStatus), query.Status);
            }
            q = q.Where(j => j.Status == status);
        }
        else if (query.IncludeInactive == true)
        {
            if (!isAdmin)
            {
                return Results.Problem(
                    "Only admins may include inactive jobs.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
            // Admins see everything.
        }
        else
        {
            // Public view: approved and not soft-deleted.
            q = q.Where(j => j.Status == JobStatus.Published && j.IsActive);
        }

        // Search (case-insensitive substring on the human fields).
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = $"%{EscapeLike(query.Q)}%";
            q = q.Where(j => EF.Functions.ILike(j.Title, pattern, "\\")
                || EF.Functions.ILike(j.Company, pattern, "\\")
                || EF.Functions.ILike(j.Description, pattern, "\\"));
        }

        // Filters.
        if (!string.IsNullOrWhiteSpace(query.JobType))
        {
            if (!JobTypes.TryParse(query.JobType, out var jobType))
            {
                return EnumError(typeof(JobType), query.JobType);
            }
            q = q.Where(j => j.JobType == jobType);
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var pattern = $"%{EscapeLike(query.Location)}%";
            q = q.Where(j => EF.Functions.ILike(j.Location, pattern, "\\"));
        }

        // Pagination metadata counts the filtered set (before sorting/paging).
        var totalCount = await q.CountAsync();

        // Sorting (whitelist — unknown values fall back to newest).
        q = (query.Sort ?? "newest").ToLowerInvariant() switch
        {
            "oldest" => q.OrderBy(j => j.PublishedAt ?? j.CreatedAt),
            "title_asc" => q.OrderBy(j => j.Title),
            "title_desc" => q.OrderByDescending(j => j.Title),
            "deadline" => q.OrderBy(j => j.Deadline ?? DateTimeOffset.MaxValue),
            _ => q.OrderByDescending(j => j.PublishedAt ?? j.CreatedAt),
        };

        // Cap the page so (page - 1) * pageSize can never overflow int.
        var page = Math.Clamp(query.Page ?? 1, 1, 100_000);
        var pageSize = Math.Clamp(query.PageSize ?? 10, 1, MaxPageSize);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return Results.Ok(new JobListData(
            items.Select(j => ToResponse(j, user.Id)).ToList(),
            new PaginationResponse(page, pageSize, totalCount, totalPages, page < totalPages)));
    }

    /// <summary>GET /api/jobs/{id} — detail. Drafts/pending are only visible to their owner or admins.</summary>
    [Authorize]
    private static async Task<IResult> GetJobAsync(
        Guid id,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }

        var isOwner = job.PostedByUserId == user.Id;
        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        var isLive = job.Status == JobStatus.Published && job.IsActive;

        // 404 (not 403) so hidden jobs don't leak their existence.
        if (!isLive && !isOwner && !isAdmin)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(job, user.Id));
    }

    /// <summary>POST /api/jobs — create a job (recruiter or admin). Draft by default.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> CreateJobAsync(
        JobWriteRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var (title, error) = ValidateTitle(request.Title);
        if (error is not null)
        {
            return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
        }
        // Blank jobType = unset (Other), matching the codebase's blank-enum convention.
        var jobType = JobType.Other;
        if (!string.IsNullOrWhiteSpace(request.JobType) && !JobTypes.TryParse(request.JobType, out jobType))
        {
            return EnumError(typeof(JobType), request.JobType);
        }
        if (!string.IsNullOrWhiteSpace(request.Url)
            && !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return Results.Problem("Url must be an absolute URL (e.g. https://...).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        var now = DateTimeOffset.UtcNow;

        var status = request.SaveAsDraft
            ? JobStatus.Draft
            : isAdmin
                ? JobStatus.Published   // admins skip the queue
                : JobStatus.PendingApproval;

        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = request.Description?.Trim() ?? string.Empty,
            Company = request.Company?.Trim() ?? string.Empty,
            Location = request.Location?.Trim() ?? string.Empty,
            JobType = jobType,
            Url = request.Url?.Trim() ?? string.Empty,
            Salary = request.Salary?.Trim() ?? string.Empty,
            PublishedAt = request.PublishedAt,
            Deadline = request.Deadline,
            Status = status,
            IsActive = status == JobStatus.Published,
            PostedByUserId = user.Id,
            SubmittedAt = status is JobStatus.PendingApproval or JobStatus.Published ? now : null,
            ApprovedAt = status == JobStatus.Published ? now : null,
            RejectedAt = null,
            RejectionReason = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        return Results.Created($"/api/jobs/{job.Id}", ToResponse(job, user.Id));
    }

    /// <summary>PUT /api/jobs/{id} — full update. Owners edit non-published jobs; admins edit anything.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> UpdateJobAsync(
        Guid id,
        JobWriteRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        if (!isAdmin)
        {
            if (job.PostedByUserId != user.Id)
            {
                return Results.Problem("You can only edit jobs you posted.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
            if (job.Status == JobStatus.Published)
            {
                return Results.Problem("Published jobs can't be edited — ask an admin.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
        }

        var (title, error) = ValidateTitle(request.Title);
        if (error is not null)
        {
            return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
        }
        // Blank jobType = unset (Other), matching the codebase's blank-enum convention.
        var jobType = JobType.Other;
        if (!string.IsNullOrWhiteSpace(request.JobType) && !JobTypes.TryParse(request.JobType, out jobType))
        {
            return EnumError(typeof(JobType), request.JobType);
        }
        if (!string.IsNullOrWhiteSpace(request.Url)
            && !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return Results.Problem("Url must be an absolute URL (e.g. https://...).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        job.Title = title;
        job.Description = request.Description?.Trim() ?? string.Empty;
        job.Company = request.Company?.Trim() ?? string.Empty;
        job.Location = request.Location?.Trim() ?? string.Empty;
        job.JobType = jobType;
        job.Url = request.Url?.Trim() ?? string.Empty;
        job.Salary = request.Salary?.Trim() ?? string.Empty;
        job.PublishedAt = request.PublishedAt;
        job.Deadline = request.Deadline;
        job.UpdatedAt = DateTimeOffset.UtcNow;

        // Editing a rejected job resets it to a draft so the owner can re-submit.
        if (job.Status == JobStatus.Rejected)
        {
            job.Status = JobStatus.Draft;
            job.RejectedAt = null;
            job.SubmittedAt = null;
            job.RejectionReason = string.Empty;
        }

        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(job, user.Id));
    }

    /// <summary>DELETE /api/jobs/{id} — soft delete (is_active=false). Admins pass ?hard=true for permanent.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> DeleteJobAsync(
        Guid id,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        bool hard = false)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        if (!isAdmin && job.PostedByUserId != user.Id)
        {
            return Results.Problem("You can only delete jobs you posted.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (hard)
        {
            // Admins hard-delete anything; owners may hard-delete their own
            // drafts (drafts are never public, so a permanent remove is safe).
            var canHardDelete =
                isAdmin || (job.PostedByUserId == user.Id && job.Status == JobStatus.Draft);
            if (!canHardDelete)
            {
                return Results.Problem(
                    "Only admins can hard-delete jobs; owners may only hard-delete their own drafts.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
            db.Jobs.Remove(job);
        }
        else
        {
            job.IsActive = false;
            job.UpdatedAt = DateTimeOffset.UtcNow;

            // Soft-deleting a pending job withdraws it from the review queue
            // (back to Draft, re-submittable) so it can't be approved while hidden.
            if (job.Status == JobStatus.PendingApproval)
            {
                job.Status = JobStatus.Draft;
                job.SubmittedAt = null;
            }
        }

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    /// <summary>PATCH /api/jobs/{id}/submit — owner sends a draft (or rejected) job in for review.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> SubmitJobAsync(
        Guid id,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        if (!isAdmin && job.PostedByUserId != user.Id)
        {
            return Results.Problem("You can only submit jobs you posted.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (job.Status == JobStatus.PendingApproval)
        {
            return Results.Problem("This job is already waiting for review.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        if (job.Status == JobStatus.Published)
        {
            return Results.Problem("This job is already published.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var now = DateTimeOffset.UtcNow;
        job.Status = JobStatus.PendingApproval;
        job.SubmittedAt = now;
        // Clear stale rejection data — a resubmission starts fresh.
        job.RejectedAt = null;
        job.RejectionReason = string.Empty;
        job.UpdatedAt = now;

        await db.SaveChangesAsync();
        return Results.Ok(ToResponse(job, user.Id));
    }

    /// <summary>PATCH /api/jobs/{id}/approve — admin publishes a pending job (flips is_active).</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> ApproveJobAsync(
        Guid id,
        ApplicationDbContext db)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }
        if (job.Status != JobStatus.PendingApproval)
        {
            return Results.Problem("Only pending jobs can be approved.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var now = DateTimeOffset.UtcNow;
        job.Status = JobStatus.Published;
        job.ApprovedAt = now;
        job.IsActive = true;
        job.UpdatedAt = now;

        await db.SaveChangesAsync();
        return Results.Ok(ToResponse(job, string.Empty));
    }

    /// <summary>PATCH /api/jobs/{id}/reject — admin rejects a pending job with a reason.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> RejectJobAsync(
        Guid id,
        [FromBody] RejectJobRequest request,
        ApplicationDbContext db)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }
        if (job.Status != JobStatus.PendingApproval)
        {
            return Results.Problem("Only pending jobs can be rejected.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var now = DateTimeOffset.UtcNow;
        job.Status = JobStatus.Rejected;
        job.RejectedAt = now;
        job.RejectionReason = request.Reason?.Trim() ?? string.Empty;
        job.IsActive = false;
        job.UpdatedAt = now;

        await db.SaveChangesAsync();
        return Results.Ok(ToResponse(job, string.Empty));
    }

    /// <summary>PATCH /api/jobs/{id}/restore — admin unhides a soft-deleted job (is_active=true).</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> RestoreJobAsync(
        Guid id,
        ApplicationDbContext db)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }

        job.IsActive = true;
        job.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(ToResponse(job, string.Empty));
    }

    // --------------------------------------------------------------- Helpers

    private static JobResponse ToResponse(Job job, string userId) => new(
        job.Id,
        job.Title,
        job.Description,
        job.Company,
        job.Location,
        EnumCamel(job.JobType.ToString()),
        job.Url,
        job.Salary,
        FormatDate(job.PublishedAt),
        FormatDate(job.Deadline),
        EnumCamel(job.Status.ToString()),
        job.IsActive,
        job.PostedByUserId == userId,
        string.IsNullOrEmpty(job.RejectionReason) ? null : job.RejectionReason,
        FormatDate(job.CreatedAt),
        FormatDate(job.UpdatedAt));

    /// <summary>"FullTime" → "fullTime" — enum values follow the JSON camelCase keys.</summary>
    private static string EnumCamel(string enumName) =>
        char.ToLowerInvariant(enumName[0]) + enumName[1..];

    /// <summary>One date format everywhere: UTC, seconds precision, Z suffix.</summary>
    private const string UtcDateFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static string FormatDate(DateTimeOffset value) =>
        value.ToUniversalTime().ToString(UtcDateFormat, CultureInfo.InvariantCulture);

    private static string? FormatDate(DateTimeOffset? value) =>
        value.HasValue ? FormatDate(value.Value) : null;

    private static (string Value, string? Error) ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (string.Empty, "Title is required.");
        }
        var trimmed = title.Trim();
        if (trimmed.Length > 500)
        {
            return (string.Empty, "Title must be 500 characters or fewer.");
        }
        return (trimmed, null);
    }

    /// <summary>Escapes LIKE wildcards so user input is matched literally.</summary>
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static bool TryParseJobStatus(string? value, out JobStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    private static IResult EnumError(Type enumType, string? value) =>
        Results.Problem(
            $"Invalid value '{value}' for {enumType.Name}. Valid values: {(enumType == typeof(JobType) ? JobTypes.ValidValuesDescription : string.Join(", ", Enum.GetNames(enumType)))}.",
            statusCode: StatusCodes.Status400BadRequest);
}
