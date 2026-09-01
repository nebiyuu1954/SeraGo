using System.Globalization;
using System.Security.Claims;
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
        group.MapGet("/locations", ListLocationsAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        group.MapGet("/{id:guid}", GetJobAsync).RequireRateLimiting("jobs_read").WithOpenApi();

        group.MapPost("/", CreateJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapPut("/{id:guid}", UpdateJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapDelete("/{id:guid}", DeleteJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();

        group.MapPatch("/{id:guid}/submit", SubmitJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapPatch("/{id:guid}/approve", ApproveJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapPatch("/{id:guid}/reject", RejectJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapPatch("/{id:guid}/restore", RestoreJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapPatch("/{id:guid}/sector", SetJobSectorAsync).RequireRateLimiting("jobs_write").WithOpenApi();

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
        public Guid? SectorId { get; set; }     // canonical sector filter
        public bool? ForMe { get; set; }        // personalize to the caller's preferred sectors
        public string? Sort { get; set; }       // newest | oldest | title_asc | title_desc | deadline

        // Extra filters (talent browse) — all optional.
        public string? Source { get; set; }          // comma-separated source display names, e.g. "Afriwork,EthioJobs"
        public string? ExperienceLevel { get; set; } // exact match, e.g. "Junior" / "Senior"
        public string? WorkMode { get; set; }        // work arrangement: onsite | remote | hybrid
        public int? PostedWithin { get; set; }       // days: published_at >= now - N
        public int? ClosingWithin { get; set; }      // days: deadline within the next N days

        // Structured salary range filters
        public decimal? SalaryMin { get; set; }       // minimum salary (inclusive)
        public decimal? SalaryMax { get; set; }       // maximum salary (inclusive)
        public string? SalaryCurrency { get; set; }   // ETB / USD

        // Structured experience range filters
        public int? ExperienceMinYears { get; set; }  // minimum years of experience
        public int? ExperienceMaxYears { get; set; }  // maximum years of experience

        // Scope (recruiter/admin) — nullable so the binder never treats a
        // missing parameter as required.
        public bool? Mine { get; set; }            // my own jobs (any status) — owner only
        public string? Status { get; set; }        // status filter — admin (or with Mine)
        public bool? IncludeInactive { get; set; } // include hidden jobs — admin only
        public bool? Uncategorized { get; set; }   // jobs without a sector — admin only

        // Pagination
        public int? Page { get; set; }          // default 1
        public int? PageSize { get; set; }      // default 10
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

        // Source attribution — normally only set by admin/manual imports (the
        // scraper pipeline). Omitted on regular recruiter posts, which the
        // create handler attributes to "SeraGo" itself.
        public string? SourceName { get; set; }
        public string? SourceUrl { get; set; }
        public string? ExternalId { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public Guid? SectorId { get; set; }
        public string? SectorName { get; set; }
        public string? ExperienceLevel { get; set; }

        /// <summary>Work arrangement: "onsite" | "remote" | "hybrid" (blank = onsite).</summary>
        public string? WorkMode { get; set; }

        // Structured salary range
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? SalaryCurrency { get; set; }
        public string? SalaryPeriod { get; set; }

        // Structured experience range
        public int? ExperienceMinYears { get; set; }
        public int? ExperienceMaxYears { get; set; }

        // Hiring
        public int? NumberOfPositions { get; set; }
    }

    public sealed record SetJobSectorRequest(Guid? SectorId);

    public sealed record RejectJobRequest(string? Reason);

    public sealed record JobResponse(
        Guid Id,
        string Title,
        string Description,
        string Company,
        string Location,
        string JobType,          // lowerCamel enum name, e.g. "fullTime"
        string WorkMode,         // lowerCamel enum name, e.g. "remote" / "hybrid" / "onsite"
        string Url,
        string Salary,
        decimal? SalaryMin,
        decimal? SalaryMax,
        string? SalaryCurrency,
        string? SalaryPeriod,
        int? ExperienceMinYears,
        int? ExperienceMaxYears,
        int NumberOfPositions,
        string? PublishedAt,     // UTC ISO-8601, e.g. "2026-09-15T14:00:00Z"
        string? RefreshedAt,     // UTC ISO-8601 — when the source last refreshed the listing
        string? Deadline,
        string Status,           // lowerCamel enum name, e.g. "draft"
        bool IsActive,
        bool IsOwner,
        string? RejectionReason, // null when empty
        string CreatedAt,
        string UpdatedAt,
        // Source attribution — null for jobs posted directly on SeraGo.
        string? SourceName,
        string? SourceUrl,
        string? ExternalId,
        string? CompanyLogoUrl,
        Guid? SectorId,
        string? SectorName,
        string? ExperienceLevel,
        string? Skills,
        // Source-specific rendering fields — populated from per-site scraper models.
        // Afriwork
        string? SourceSectors,
        int? CompensationAmountCents,
        string? CompensationType,
        string? CompensationCurrency,
        string? EntityType,
        // EthioJobs
        string? SourceCategories,
        // Shared (EthioJobs + HaHu)
        string? ApplicationMethod,
        string? ApplicationEmail,
        string? ApplicationUrl,
        // HaHuJobs
        string? UpstreamSource,
        string? AreaName,
        string? SubSectorName,
        int? NumberOfApplicants,
        // GeezJobs
        string? EmploymentText,
        string? JobTime,
        string? SiteJobType,
        string? ExperienceText,
        int? MaxExperienceYears,
        string? PostedText,
        string? DeadlineText,
        // ReporterJobs
        string? JobTypeText,
        // Analytics
        int ViewCount);

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

        var q = db.Jobs.AsNoTracking()
            .Include(j => j.PostedBy)
            .AsQueryable();

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
        else if (query.Uncategorized == true)
        {
            // Admin review queue: jobs the normalizer couldn't categorize.
            if (!isAdmin)
            {
                return Results.Problem(
                    "Only admins may list uncategorized jobs.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
            q = q.Where(j => j.SectorId == null);
        }
        else
        {
            // Public view: approved, not soft-deleted, and still within its
            // lifecycle window (deadline + 7 days — the shared rule from
            // JobLifecycle). Past-window jobs leave the feed here and are
            // deleted by the weekly cleanup. Jobs without a deadline stay.
            q = q.Where(j => j.Status == JobStatus.Published && j.IsActive
                && (j.Deadline == null
                    || j.Deadline > DateTimeOffset.UtcNow.AddDays(-JobLifecycle.GraceDays)));
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
            var location = query.Location.Trim();
            if (location.Equals("Remote", StringComparison.OrdinalIgnoreCase))
            {
                // "Remote" is both a location text AND a work mode — match
                // remote/hybrid jobs wherever they say they're based.
                q = q.Where(j => EF.Functions.ILike(j.Location, "%remote%", "\\")
                    || j.WorkMode == WorkMode.Remote
                    || j.WorkMode == WorkMode.Hybrid);
            }
            else
            {
                var pattern = $"%{EscapeLike(location)}%";
                q = q.Where(j => EF.Functions.ILike(j.Location, pattern, "\\"));
            }
        }

        // Personalized feed — only the caller's preferred sectors.
        if (query.ForMe == true)
        {
            var profile = await db.TalentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            var prefs = profile?.PreferredSectorIds ?? [];
            // No preferences → nothing matches (the frontend shows the setup prompt).
            q = prefs.Count > 0
                ? q.Where(j => j.SectorId != null && prefs.Contains(j.SectorId.Value))
                : q.Where(j => false);
        }

        if (query.SectorId is not null)
        {
            q = q.Where(j => j.SectorId == query.SectorId);
        }

        // Talent-browse filters.
        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var sources = query.Source.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            q = q.Where(j => j.SourceName != null && sources.Contains(j.SourceName));
        }

        if (!string.IsNullOrWhiteSpace(query.ExperienceLevel))
        {
            q = q.Where(j => j.ExperienceLevel == query.ExperienceLevel.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.WorkMode))
        {
            if (!TryParseWorkMode(query.WorkMode, out var workMode))
            {
                return EnumError(typeof(WorkMode), query.WorkMode);
            }
            q = q.Where(j => j.WorkMode == workMode);
        }

        if (query.PostedWithin is > 0)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-query.PostedWithin.Value);
            q = q.Where(j => j.PublishedAt != null && j.PublishedAt >= cutoff);
        }

        if (query.ClosingWithin is > 0)
        {
            var now = DateTimeOffset.UtcNow;
            var horizon = now.AddDays(query.ClosingWithin.Value);
            q = q.Where(j => j.Deadline != null && j.Deadline >= now && j.Deadline <= horizon);
        }

        // Structured salary range filters.
        if (query.SalaryMin is > 0)
        {
            q = q.Where(j => j.SalaryMax != null && j.SalaryMax >= query.SalaryMin.Value);
        }
        if (query.SalaryMax is > 0)
        {
            q = q.Where(j => j.SalaryMin != null && j.SalaryMin <= query.SalaryMax.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.SalaryCurrency))
        {
            q = q.Where(j => j.SalaryCurrency == query.SalaryCurrency.Trim());
        }

        // Structured experience range filters.
        if (query.ExperienceMinYears is > 0)
        {
            // Job's max must be >= the candidate's minimum.
            q = q.Where(j => j.ExperienceMaxYears == null || j.ExperienceMaxYears >= query.ExperienceMinYears.Value);
        }
        if (query.ExperienceMaxYears is > 0)
        {
            // Job's min must be <= the candidate's maximum.
            q = q.Where(j => j.ExperienceMinYears == null || j.ExperienceMinYears <= query.ExperienceMaxYears.Value);
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

    /// <summary>
    /// GET /api/jobs/locations — the locations available in the live feed
    /// (same public scope as the list: published, active, within the
    /// lifecycle window), for powering the location filter dropdown.
    ///
    /// Raw values are normalized so the dropdown shows searchable places, not
    /// the sources' messy strings:
    ///   - multi-city listings ("Addis Ababa, Amhara, Burie, …") split into
    ///     their individual places — picking any one matches the whole job,
    ///     since the location filter is a substring match;
    ///   - junk placeholders ("Not Specified", "Others", "Ethiopia"…) are dropped;
    ///   - remote variants ("Anywhere/Remote", "Remote") fold into one "Remote";
    ///   - case variants merge into the most common spelling;
    ///   - sorted alphabetically (case-insensitive).
    /// </summary>
    [Authorize]
    private static async Task<IResult> ListLocationsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var groups = await db.Jobs.AsNoTracking()
            .Where(j => j.Status == JobStatus.Published && j.IsActive
                && j.Location != null && j.Location.Trim() != string.Empty
                && (j.Deadline == null
                    || j.Deadline > now.AddDays(-JobLifecycle.GraceDays)))
            .GroupBy(j => j.Location!.Trim())
            .Select(g => new { Location = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Every split token carries its source group's count so the
        // most-common-spelling dedupe below stays meaningful.
        var entries = new List<(string Token, int Count)>(groups.Count * 2);
        foreach (var item in groups)
        {
            foreach (var raw in item.Location.Split(','))
            {
                var token = NormalizeLocationToken(raw);
                if (token is null || JunkLocationTokens.Contains(token))
                {
                    continue;
                }
                entries.Add((token, item.Count));
            }
        }

        // Case-insensitive dedupe — keep the most common spelling (ties keep
        // the alphabetically-first one since the input is pre-sorted).
        var best = new Dictionary<string, (string Name, int Count)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries.OrderBy(e => e.Token, StringComparer.OrdinalIgnoreCase))
        {
            if (!best.TryGetValue(entry.Token, out var current) || entry.Count > current.Count)
            {
                best[entry.Token] = (entry.Token, entry.Count);
            }
        }

        return Results.Ok(best.Values
            .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .Select(v => v.Name)
            .ToList());
    }

    /// <summary>Placeholders that aren't real locations — never offered in the dropdown.</summary>
    private static readonly HashSet<string> JunkLocationTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "Not Specified", "Others", "Project", "Any", "N/A", "NA", "Ethiopia", "-", "TBD", "To Be Decided",
    };

    /// <summary>Trim / collapse whitespace, fold remote variants into "Remote", drop empties.</summary>
    private static string? NormalizeLocationToken(string raw)
    {
        var collapsed = string.Join(' ', raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length == 0)
        {
            return null;
        }
        // "Anywhere/Remote" and friends all mean remote work.
        if (collapsed.Contains("remote", StringComparison.OrdinalIgnoreCase))
        {
            return "Remote";
        }
        // Nicer display: capitalize all-lowercase tokens ("bishoftu" →
        // "Bishoftu") without touching mixed-case names like "South West
        // Ethiopia People's Region".
        if (collapsed == collapsed.ToLowerInvariant())
        {
            collapsed = char.ToUpperInvariant(collapsed[0]) + collapsed[1..];
        }
        return collapsed;
    }

    /// <summary>GET /api/jobs/{id} — detail. Drafts/pending are only visible to their owner or admins.</summary>
    [Authorize]
    private static async Task<IResult> GetJobAsync(
        Guid id,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        HttpContext http)
    {
        var user = await userManager.GetUserAsync(claims);
        var isAdmin = user is not null && await userManager.IsInRoleAsync(user, Roles.Admin);

        // Use tracked entity so we can increment ViewCount.
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }

        var isOwner = user is not null && job.PostedByUserId == user.Id;
        var isLive = job.Status == JobStatus.Published && job.IsActive;

        // 404 (not 403) so hidden jobs don't leak their existence.
        if (!isLive && !isOwner && !isAdmin)
        {
            return Results.NotFound();
        }

        // Track unique view: once per user (auth) or once per IP (anon).
        if (isLive && !isOwner && !isAdmin)
        {
            var clientIp = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            bool alreadyViewed;
            if (user is not null)
            {
                // Authenticated: dedup by (JobId, UserId).
                alreadyViewed = await db.JobViews
                    .AnyAsync(v => v.JobId == id && v.UserId == user.Id);
            }
            else
            {
                // Anonymous: dedup by (JobId, IpAddress).
                alreadyViewed = await db.JobViews
                    .AnyAsync(v => v.JobId == id && v.IpAddress == clientIp && v.UserId == null);
            }

            if (!alreadyViewed)
            {
                db.JobViews.Add(new JobView
                {
                    Id = Guid.NewGuid(),
                    JobId = id,
                    UserId = user?.Id,
                    IpAddress = user is null ? clientIp : null,
                    ViewedAt = DateTimeOffset.UtcNow,
                });
                job.ViewCount++;
                await db.SaveChangesAsync();
            }
        }

        return Results.Ok(ToResponse(job, user?.Id ?? string.Empty));
    }

    /// <summary>POST /api/jobs — create a job (recruiter or admin). Draft by default.</summary>
    [Authorize(Roles = Roles.Recruiter + "," + Roles.Admin)]
    private static async Task<IResult> CreateJobAsync(
        JobWriteRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        NotificationService notificationService)
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
        // Blank work mode = onsite, matching the codebase's blank-enum convention.
        var workMode = WorkMode.Onsite;
        if (!string.IsNullOrWhiteSpace(request.WorkMode)
            && !TryParseWorkMode(request.WorkMode, out workMode))
        {
            return EnumError(typeof(WorkMode), request.WorkMode);
        }
        if (!string.IsNullOrWhiteSpace(request.Url)
            && !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return Results.Problem("Url must be an absolute URL (e.g. https://...).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Sector is required when submitting (not drafting) — needed for job alerts
        if (!request.SaveAsDraft && request.SectorId is null)
        {
            return Results.Problem(
                "Sector is required when submitting a job for review. This helps us notify the right talents.",
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
            WorkMode = workMode,
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
            // Attribution: in-house posts are SeraGo's own; a provided
            // SourceName (admin/manual import) overrides that.
            SourceName = string.IsNullOrWhiteSpace(request.SourceName)
                ? "SeraGo"
                : request.SourceName.Trim(),
            SourceUrl = NullIfBlank(request.SourceUrl),
            ExternalId = NullIfBlank(request.ExternalId),
            CompanyLogoUrl = NullIfBlank(request.CompanyLogoUrl),
            SectorId = null,
            SectorName = NullIfBlank(request.SectorName),
            ExperienceLevel = NullIfBlank(request.ExperienceLevel),
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            SalaryCurrency = NullIfBlank(request.SalaryCurrency),
            SalaryPeriod = NullIfBlank(request.SalaryPeriod),
            ExperienceMinYears = request.ExperienceMinYears,
            ExperienceMaxYears = request.ExperienceMaxYears,
            NumberOfPositions = Math.Clamp(request.NumberOfPositions ?? 1, 1, 999),
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Sector: validate the id and default the display name to the canonical one.
        if (request.SectorId is not null)
        {
            var sector = await db.Sectors.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.SectorId.Value);
            if (sector is null)
            {
                return Results.Problem("SectorId doesn't exist.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            job.SectorId = sector.Id;
            job.SectorName = string.IsNullOrWhiteSpace(request.SectorName)
                ? sector.Name
                : request.SectorName.Trim();
        }

        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        // If job was published immediately (admin), notify matching talents
        if (status == JobStatus.Published)
        {
            await NotifyMatchingTalentsAsync(db, notificationService, job);
        }

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
        // Blank work mode = onsite, matching the codebase's blank-enum convention.
        var workMode = WorkMode.Onsite;
        if (!string.IsNullOrWhiteSpace(request.WorkMode)
            && !TryParseWorkMode(request.WorkMode, out workMode))
        {
            return EnumError(typeof(WorkMode), request.WorkMode);
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
        job.WorkMode = workMode;
        job.Url = request.Url?.Trim() ?? string.Empty;
        job.Salary = request.Salary?.Trim() ?? string.Empty;
        job.PublishedAt = request.PublishedAt;
        job.Deadline = request.Deadline;

        // Structured salary / experience / positions
        job.SalaryMin = request.SalaryMin;
        job.SalaryMax = request.SalaryMax;
        job.SalaryCurrency = NullIfBlank(request.SalaryCurrency);
        job.SalaryPeriod = NullIfBlank(request.SalaryPeriod);
        job.ExperienceMinYears = request.ExperienceMinYears;
        job.ExperienceMaxYears = request.ExperienceMaxYears;
        if (request.NumberOfPositions is not null)
        {
            job.NumberOfPositions = Math.Clamp(request.NumberOfPositions.Value, 1, 999);
        }

        // Sector: set when provided (and only then — a recruiter editing their
        // own post must never wipe an imported sector by omitting the field).
        if (request.SectorId is not null)
        {
            var sector = await db.Sectors.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.SectorId.Value);
            if (sector is null)
            {
                return Results.Problem("SectorId doesn't exist.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            job.SectorId = sector.Id;
            job.SectorName = string.IsNullOrWhiteSpace(request.SectorName)
                ? sector.Name
                : request.SectorName.Trim();
        }

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
        ApplicationDbContext db,
        NotificationService notificationService)
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
        job.UpdatedAt = now;        await db.SaveChangesAsync();

        // Notify the recruiter their job was approved
        if (job.PostedByUserId is not null)
        {
            await notificationService.CreateAsync(new NotificationService.CreateNotificationRequest(
                UserId: job.PostedByUserId,
                Type: NotificationType.AdminReviewResult,
                Title: "Job posting approved",
                Body: $"Your job \"{job.Title}\" is now live and visible to talents.",
                Data: $"{{\"job_id\":\"{job.Id}\",\"status\":\"approved\"}}",
                SkipEmail: false,
                SkipTelegram: true
            ));
        }

        // Notify matching talents about the new job
        await NotifyMatchingTalentsAsync(db, notificationService, job);

        return Results.Ok(ToResponse(job, string.Empty));
    }


    /// <summary>PATCH /api/jobs/{id}/reject — admin rejects a pending job with a reason.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> RejectJobAsync(
        Guid id,
        [FromBody] RejectJobRequest request,
        ApplicationDbContext db,
        NotificationService notificationService)
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

        // Notify the recruiter their job was rejected
        if (job.PostedByUserId is not null)
        {
            var reason = string.IsNullOrWhiteSpace(job.RejectionReason)
                ? "No reason provided"
                : job.RejectionReason;

            await notificationService.CreateAsync(new NotificationService.CreateNotificationRequest(
                UserId: job.PostedByUserId,
                Type: NotificationType.AdminReviewResult,
                Title: "Job posting rejected",
                Body: $"Your job \"{job.Title}\" was rejected. Reason: {reason}",
                Data: $"{{\"job_id\":\"{job.Id}\",\"status\":\"rejected\",\"reason\":\"{reason.Replace("\"", "\\\"")}\"}}",
                SkipEmail: false,
                SkipTelegram: true
            ));
        }

        return Results.Ok(ToResponse(job, string.Empty));
    }

    /// <summary>PATCH /api/jobs/{id}/sector — admin (re)assigns or clears a job's sector (null clears).</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> SetJobSectorAsync(
        Guid id,
        SetJobSectorRequest request,
        ApplicationDbContext db)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }

        Sector? sector = null;
        if (request.SectorId is not null)
        {
            sector = await db.Sectors.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.SectorId.Value);
            if (sector is null)
            {
                return Results.Problem("SectorId doesn't exist.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        job.SectorId = sector?.Id;
        job.SectorName = sector?.Name;
        job.UpdatedAt = DateTimeOffset.UtcNow;

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
        EnumCamel(job.WorkMode.ToString()),
        job.Url,
        job.Salary,
        job.SalaryMin,
        job.SalaryMax,
        job.SalaryCurrency,
        job.SalaryPeriod,
        job.ExperienceMinYears,
        job.ExperienceMaxYears,
        job.NumberOfPositions,
        FormatDate(job.PublishedAt),
        FormatDate(job.RefreshedAt),
        FormatDate(job.Deadline),
        EnumCamel(job.Status.ToString()),
        job.IsActive,
        job.PostedByUserId == userId,
        string.IsNullOrEmpty(job.RejectionReason) ? null : job.RejectionReason,
        FormatDate(job.CreatedAt),
        FormatDate(job.UpdatedAt),
        job.SourceName,
        job.SourceUrl,
        job.ExternalId,
        NullIfBlank(job.CompanyLogoUrl) ?? NullIfBlank(job.PostedBy?.AvatarUrl),
        job.SectorId,
        job.SectorName,
        job.ExperienceLevel,
        job.Skills,
        // Source-specific rendering fields
        job.SourceSectors,
        job.CompensationAmountCents,
        job.CompensationType,
        job.CompensationCurrency,
        job.EntityType,
        job.SourceCategories,
        job.ApplicationMethod,
        job.ApplicationEmail,
        job.ApplicationUrl,
        job.UpstreamSource,
        job.AreaName,
        job.SubSectorName,
        job.NumberOfApplicants,
        job.EmploymentText,
        job.JobTime,
        job.SiteJobType,
        job.ExperienceText,
        job.MaxExperienceYears,
        job.PostedText,
        job.DeadlineText,
        job.JobTypeText,
        job.ViewCount);

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

    /// <summary>Blank strings become null — matches the "null when empty" API contract.</summary>
    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Escapes LIKE wildcards so user input is matched literally.</summary>
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static bool TryParseJobStatus(string? value, out JobStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    /// <summary>"onsite" | "remote" | "hybrid" → WorkMode (case-insensitive).</summary>
    private static bool TryParseWorkMode(string? value, out WorkMode workMode)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "onsite":
                workMode = WorkMode.Onsite;
                return true;
            case "remote":
                workMode = WorkMode.Remote;
                return true;
            case "hybrid":
                workMode = WorkMode.Hybrid;
                return true;
            default:
                workMode = WorkMode.Onsite;
                return false;
        }
    }

    private static IResult EnumError(Type enumType, string? value) =>
        Results.Problem(
            $"Invalid value '{value}' for {enumType.Name}. Valid values: {(enumType == typeof(JobType) ? JobTypes.ValidValuesDescription : string.Join(", ", Enum.GetNames(enumType)))}.",
            statusCode: StatusCodes.Status400BadRequest);

    /// <summary>
    /// Notify all talents whose sector preference matches the new job.
    /// In production, the Django AI service handles this via the batch endpoint.
    /// This is the fallback for SeraGo-posted jobs.
    /// </summary>
    private static async Task NotifyMatchingTalentsAsync(
        ApplicationDbContext db,
        NotificationService notificationService,
        Job job)
    {
        try
        {
            // Find talents who have this job's sector in their preferences
            IQueryable<string> matchingUserIds;

            if (job.SectorId is not null)
            {
                // Match talents whose preferred sectors include this job's sector
                matchingUserIds = db.TalentProfiles
                    .Where(tp => tp.PreferredSectorIds.Contains(job.SectorId.Value))
                    .Select(tp => tp.UserId);
            }
            else
            {
                // No sector info — notify all active talents
                matchingUserIds = db.Users
                    .Where(u => u.UserType == Core.Domain.Enums.UserType.Talent && u.IsActive)
                    .Select(u => u.Id);
            }

            var userIds = await matchingUserIds.Take(500).ToListAsync(); // cap at 500

            if (userIds.Count == 0) return;

            // Create individual notifications (not batch — these are real-time alerts)
            foreach (var userId in userIds)
            {
                await notificationService.CreateAsync(new NotificationService.CreateNotificationRequest(
                    UserId: userId,
                    Type: NotificationType.JobAlert,
                    Title: "New job posted",
                    Body: $"{job.Title} at {job.Company} in {job.Location}",
                    Data: $"{{\"job_id\":\"{job.Id}\",\"sector\":\"{job.SectorName ?? "General"}\"}}",
                    SkipEmail: true, // defer email to digest
                    SkipTelegram: true
                ));
            }
        }
        catch (Exception ex)
        {
            // Don't fail the request if notifications fail
            // Log would go here in production
        }
    }
}
