using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Services;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Sectors — the canonical job-sector vocabulary — plus the admin tooling
/// around it: managing sectors/aliases, the uncategorized review data, and
/// the sync that imports scraped jobs from the scraper database.
///
///   GET  /api/sectors                      — active sectors (any signed-in user)
///   GET  /api/admin/sectors                — all sectors + aliases (admin)
///   POST /api/admin/sectors                — create a sector (admin)
///   PUT  /api/admin/sectors/{id}           — rename / deactivate (admin)
///   DELETE /api/admin/sectors/{id}         — delete (only when unreferenced)
///   POST /api/admin/sectors/{id}/aliases   — add aliases (admin)
///   DELETE /api/admin/sectors/{sid}/aliases/{aid} — remove an alias (admin)
///   POST /api/admin/sync/scraped-jobs      — import + normalize scraped jobs (admin)
/// </summary>
public static class SectorEndpoints
{
    /// <summary>Public sector list — always registered (any signed-in user).</summary>
    public static IEndpointRouteBuilder MapSectorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sectors").WithTags("Sectors");
        group.MapGet("/", ListSectorsAsync).RequireRateLimiting("jobs_read").WithOpenApi();

        return app;
    }

    /// <summary>Admin sector management + sync — only registered when ADMIN_API_ENABLED is on.</summary>
    public static IEndpointRouteBuilder MapAdminSectorEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin/sectors").WithTags("Sectors (admin)");
        admin.MapGet("/", ListAdminSectorsAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        admin.MapPost("/", CreateSectorAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapPut("/{id:guid}", UpdateSectorAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapDelete("/{id:guid}", DeleteSectorAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapPost("/{id:guid}/aliases", AddAliasesAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapDelete("/{sectorId:guid}/aliases/{aliasId:guid}", RemoveAliasAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapGet("/classification-history", GetClassificationHistoryAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        admin.MapPost("/audit-classification", AuditClassificationAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapPost("/apply-audit-corrections", ApplyAuditCorrectionsAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapGet("/provider-fallbacks", GetProviderFallbacksAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        admin.MapGet("/dashboard-stats", GetDashboardStatsAsync).RequireRateLimiting("jobs_read").WithOpenApi();

        app.MapPost("/api/admin/sync/scraped-jobs", SyncScrapedJobsAsync)
            .RequireRateLimiting("jobs_write").WithTags("Sectors (admin)").WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record SectorResponse(Guid Id, string Name, string Slug, bool IsActive, int JobCount = 0);

    public sealed record SectorDetailResponse(
        Guid Id, string Name, string Slug, bool IsActive,
        int JobCount, List<AliasResponse> Aliases);

    public sealed record AliasResponse(Guid Id, string Alias);

    public sealed record SectorWriteRequest(string? Name, bool? IsActive, string? Slug);

    public sealed record AddAliasesRequest(List<string> Aliases);

    public sealed record ClassificationDashboardStatsResponse(
        string Date,
        TierMetricDto Tier1OpenRouter,
        TierMetricDto Tier2Groq,
        TierMetricDto Tier3Gemini,
        TierMetricDto Tier4FastPass,
        TierMetricDto Tier5SafeFallback,
        DailyOperationalMetricsDto DailyMetrics
    );

    public sealed record TierMetricDto(
        string TierName,
        string ProviderName,
        int JobsClassified,
        int JobsFailed,
        int SuccessfulRuns,
        int FailedRuns,
        string Status
    );

    public sealed record DailyOperationalMetricsDto(
        int TotalClassified,
        int TotalUnclassified,
        double AverageConfidence,
        int TotalInsertedOrSynced,
        double SuccessRatePercentage
    );

    // -------------------------------------------------------------- Handlers

    /// <summary>GET /api/sectors — active sectors with real active job counts (public).</summary>
    private static async Task<IResult> ListSectorsAsync(ApplicationDbContext db, HttpContext http)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-JobLifecycle.GraceDays);
        var countsBySectorId = await db.Jobs.AsNoTracking()
            .Where(j => j.Status == JobStatus.Published && j.IsActive && j.SectorId != null
                && (j.Deadline == null || j.Deadline > cutoff))
            .GroupBy(j => j.SectorId!.Value)
            .Select(g => new { SectorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.SectorId, g => g.Count);

        var sectors = await db.Sectors.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Slug, s.IsActive })
            .ToListAsync();

        var responses = sectors
            .Select(s => new SectorResponse(
                s.Id,
                s.Name,
                s.Slug,
                s.IsActive,
                countsBySectorId.GetValueOrDefault(s.Id, 0)))
            .OrderByDescending(s => s.JobCount)
            .ThenBy(s => s.Name)
            .ToList();

        var etag = HttpCache.ComputeEtag(responses);
        HttpCache.Apply(http.Response, etag, TimeSpan.FromMinutes(5));
        if (HttpCache.IsNotModified(http.Request, etag))
        {
            return HttpCache.NotModified();
        }

        return Results.Ok(responses);
    }

    /// <summary>GET /api/admin/sectors — everything, with aliases and job counts.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> ListAdminSectorsAsync(ApplicationDbContext db)
    {
        var sectors = await db.Sectors.AsNoTracking()
            .Include(s => s.Aliases)
            .OrderBy(s => s.Name)
            .ToListAsync();
        var counts = await db.Jobs.AsNoTracking()
            .Where(j => j.SectorId != null)
            .GroupBy(j => j.SectorId!.Value)
            .Select(g => new { SectorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SectorId, x => x.Count);

        return Results.Ok(sectors.Select(s => new SectorDetailResponse(
            s.Id, s.Name, s.Slug, s.IsActive,
            counts.TryGetValue(s.Id, out var c) ? c : 0,
            s.Aliases.OrderBy(a => a.Alias)
                .Select(a => new AliasResponse(a.Id, a.Alias))
                .ToList())));
    }

    /// <summary>POST /api/admin/sectors — create a sector (name required; slug auto-generated).</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> CreateSectorAsync(
        SectorWriteRequest request, ApplicationDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.Problem("Name is required.", statusCode: StatusCodes.Status400BadRequest);
        }
        var name = request.Name.Trim();
        if (await db.Sectors.AnyAsync(s => s.Name == name))
        {
            return Results.Problem($"A sector named '{name}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slugify(name)
            : Slugify(request.Slug);

        var sector = new Sector { Id = Guid.NewGuid(), Name = name, Slug = slug };
        db.Sectors.Add(sector);
        await db.SaveChangesAsync();
        return Results.Created($"/api/admin/sectors/{sector.Id}", new SectorResponse(sector.Id, sector.Name, sector.Slug, sector.IsActive));
    }

    /// <summary>PUT /api/admin/sectors/{id} — rename and/or deactivate.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> UpdateSectorAsync(
        Guid id, SectorWriteRequest request, ApplicationDbContext db)
    {
        var sector = await db.Sectors.FirstOrDefaultAsync(s => s.Id == id);
        if (sector is null)
        {
            return Results.NotFound();
        }
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            if (await db.Sectors.AnyAsync(s => s.Name == name && s.Id != id))
            {
                return Results.Problem($"A sector named '{name}' already exists.",
                    statusCode: StatusCodes.Status409Conflict);
            }
            sector.Name = name;
        }
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            sector.Slug = Slugify(request.Slug);
        }
        if (request.IsActive is not null)
        {
            sector.IsActive = request.IsActive.Value;
        }
        sector.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new SectorResponse(sector.Id, sector.Name, sector.Slug, sector.IsActive));
    }

    /// <summary>DELETE /api/admin/sectors/{id} — only when nothing references it.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> DeleteSectorAsync(Guid id, ApplicationDbContext db)
    {
        var sector = await db.Sectors.FirstOrDefaultAsync(s => s.Id == id);
        if (sector is null)
        {
            return Results.NotFound();
        }
        if (await db.Jobs.AnyAsync(j => j.SectorId == id))
        {
            return Results.Problem(
                "Sectors that have jobs can't be deleted — deactivate it instead.",
                statusCode: StatusCodes.Status409Conflict);
        }
        db.Sectors.Remove(sector);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    /// <summary>POST /api/admin/sectors/{id}/aliases — add raw names that map here.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> AddAliasesAsync(
        Guid id, AddAliasesRequest request, ApplicationDbContext db)
    {
        var sector = await db.Sectors.FirstOrDefaultAsync(s => s.Id == id);
        if (sector is null)
        {
            return Results.NotFound();
        }

        var added = new List<SectorAlias>();
        foreach (var raw in request.Aliases ?? [])
        {
            var alias = SectorSeedData.Normalize(raw);
            if (alias.Length == 0)
            {
                continue;
            }
            if (await db.SectorAliases.AnyAsync(a => a.Alias == alias))
            {
                continue; // already mapped somewhere
            }
            var entity = new SectorAlias { Id = Guid.NewGuid(), SectorId = id, Alias = alias };
            db.SectorAliases.Add(entity);
            added.Add(entity);
        }
        await db.SaveChangesAsync();
        return Results.Ok(added.Select(a => new AliasResponse(a.Id, a.Alias)).ToList());
    }

    /// <summary>DELETE /api/admin/sectors/{sectorId}/aliases/{aliasId}.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> RemoveAliasAsync(
        Guid sectorId, Guid aliasId, ApplicationDbContext db)
    {
        var alias = await db.SectorAliases
            .FirstOrDefaultAsync(a => a.Id == aliasId && a.SectorId == sectorId);
        if (alias is null)
        {
            return Results.NotFound();
        }
        db.SectorAliases.Remove(alias);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // ----------------------------------------------------- Scraped-job sync

    /// <summary>
    /// POST /api/admin/sync/scraped-jobs — import published jobs from the
    /// scraper database into SeraGo's Jobs table. All logic lives in
    /// <see cref="ScrapedJobSyncService"/> (also used by the auto-sync
    /// scheduler); this endpoint is the manual "do it now" trigger.
    /// Re-runs are idempotent: existing (SourceName, ExternalId) pairs are
    /// updated in place, never duplicated.
    /// </summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> SyncScrapedJobsAsync(
        ScrapedJobSyncService syncService,
        CancellationToken ct)
    {
        var result = await syncService.RunAsync("manual", ct);
        return result is null
            ? Results.Problem(
                "Scraper database is not configured (set DB_HOST/DB_NAME/DB_USER/DB_PASSWORD "
                + "in .env(SeraGo-Scraper)).",
                statusCode: StatusCodes.Status400BadRequest)
            : Results.Ok(result);
    }

    // --------------------------------------------------------------- Helpers

    private static string Slugify(string value)
    {
        var slug = value.ToLowerInvariant()
            .Replace("&", "and")
            .Replace("/", " ")
            .Replace("\\", " ");
        var builder = new System.Text.StringBuilder();
        var lastDash = false;
        foreach (var ch in slug)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastDash = false;
            }
            else if (!lastDash && builder.Length > 0)
            {
                builder.Append('-');
                lastDash = true;
            }
        }
        return builder.ToString().Trim('-');
    }

    private const string SyncQuery = """
        SELECT
            i.external_id, i.title, i.description, i.company, i.location,
            i.job_type, i.url, i.salary, i.published_at, i.deadline, i.is_active,
            s.name AS source_name, s.slug AS source_slug,
            a.sectors::text AS afriwork_sectors,
            h.sector_name AS hahu_sector, h.sub_sector_name AS hahu_sub_sector,
            h.entity_logo AS entity_logo, h.years_of_experience AS hahu_exp,
            e.catalogs::text AS ethio_catalogs,
            g.employment_text AS geez_employment, g.company_logo AS company_logo,
            g.min_experience_years AS geez_min_exp,
            r.job_type AS reporter_type
        FROM core_scrapeditem i
        JOIN core_source s ON s.id = i.source_id
        LEFT JOIN core_afriworkjob a ON a.id = i.afriwork_job_id
        LEFT JOIN core_hahujob h ON h.id = i.hahujobs_job_id
        LEFT JOIN core_ethiojobsjob e ON e.id = i.ethiojobs_job_id
        LEFT JOIN core_geezjob g ON g.id = i.geezjobs_job_id
        LEFT JOIN core_reporterjob r ON r.id = i.reporter_job_id
        WHERE i.is_active = true
        """;

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetClassificationHistoryAsync(
        ClassificationAuditService auditService,
        ILogger<ClassificationAuditService> logger,
        int? page,
        int? pageSize,
        Guid? sectorId,
        string? method,
        bool? todayOnly,
        string? date,
        string? search,
        CancellationToken ct)
    {
        try
        {
            var result = await auditService.GetHistoryAsync(
                page ?? 1,
                pageSize ?? 20,
                sectorId,
                method,
                todayOnly ?? false,
                date,
                search,
                ct);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch classification history");
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    public sealed record AuditClassificationRequest(
        List<Guid>? JobIds,
        bool? AuditTodayOnly,
        int? Limit);

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> AuditClassificationAsync(
        ClassificationAuditService auditService,
        AuditClassificationRequest body,
        CancellationToken ct)
    {
        try
        {
            var result = await auditService.AuditClassificationsAsync(
                body.JobIds,
                body.AuditTodayOnly ?? false,
                body.Limit ?? 25,
                ct);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    public sealed record ApplyAuditCorrectionsRequest(
        List<CorrectionRequestItem> Corrections);

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> ApplyAuditCorrectionsAsync(
        ClassificationAuditService auditService,
        ApplyAuditCorrectionsRequest body,
        CancellationToken ct)
    {
        if (body.Corrections is null || body.Corrections.Count == 0)
        {
            return Results.Ok(new { updated = 0, message = "No corrections provided." });
        }

        var updated = await auditService.ApplyCorrectionsAsync(body.Corrections, ct);
        return Results.Ok(new { updated, message = $"Successfully updated {updated} jobs." });
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetProviderFallbacksAsync(
        ApplicationDbContext db,
        string? date,
        CancellationToken ct)
    {
        var targetDate = string.IsNullOrWhiteSpace(date) || !DateOnly.TryParse(date, out var parsedDate)
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : parsedDate;

        var startOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero);
        var endOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc), TimeSpan.Zero);

        var events = await db.AiProviderAuditLogs
            .AsNoTracking()
            .Where(e => e.OccurredAt >= startOfDay && e.OccurredAt <= endOfDay)
            .OrderByDescending(e => e.OccurredAt)
            .Take(100)
            .Select(e => new
            {
                e.Id,
                e.OccurredAt,
                e.AttemptedTier,
                e.FailedProvider,
                e.HttpStatusCode,
                e.ErrorCategory,
                e.ErrorMessage,
                e.FallbackTier,
                e.JobCount,
                e.WasRecovered,
                e.JobTitlesSnippet
            })
            .ToListAsync(ct);

        // Calculate today's provider health metrics
        var oneHourAgo = DateTimeOffset.UtcNow.AddHours(-1);
        var recentErrors = await db.AiProviderAuditLogs
            .AsNoTracking()
            .Where(e => e.OccurredAt >= oneHourAgo)
            .ToListAsync(ct);

        var openRouterFailsLastHour = recentErrors.Count(e => e.FailedProvider.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase));
        var groqFailsLastHour = recentErrors.Count(e => e.FailedProvider.Contains("Groq", StringComparison.OrdinalIgnoreCase));
        var geminiFailsLastHour = recentErrors.Count(e => e.FailedProvider.Contains("Gemini", StringComparison.OrdinalIgnoreCase));

        // Get count of jobs classified by each model today
        var modelCountsToday = await db.JobClassificationRecords
            .AsNoTracking()
            .Where(r => r.ClassifiedAt >= startOfDay && r.ClassifiedAt <= endOfDay)
            .GroupBy(r => r.ModelUsed)
            .Select(g => new { Model = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalClassifiedToday = modelCountsToday.Sum(m => m.Count);

        var providers = new[]
        {
            new
            {
                Name = "OpenRouter Nemotron 550B",
                Tier = "Tier 1",
                Status = openRouterFailsLastHour >= 3 ? "degraded" : openRouterFailsLastHour > 0 ? "warning" : "healthy",
                JobsClassifiedToday = modelCountsToday.FirstOrDefault(m => m.Model.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase))?.Count ?? 0,
                FailoversToday = events.Count(e => e.FailedProvider.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase)),
                LastFailureReason = events.FirstOrDefault(e => e.FailedProvider.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase))?.ErrorMessage
            },
            new
            {
                Name = "Groq Llama 3.3 70B",
                Tier = "Tier 2",
                Status = groqFailsLastHour >= 3 ? "degraded" : groqFailsLastHour > 0 ? "warning" : "healthy",
                JobsClassifiedToday = modelCountsToday.FirstOrDefault(m => m.Model.Equals("Groq", StringComparison.OrdinalIgnoreCase) || m.Model.Contains("Groq Llama 3.3", StringComparison.OrdinalIgnoreCase))?.Count ?? 0,
                FailoversToday = events.Count(e => e.FailedProvider.Contains("Groq", StringComparison.OrdinalIgnoreCase)),
                LastFailureReason = events.FirstOrDefault(e => e.FailedProvider.Contains("Groq", StringComparison.OrdinalIgnoreCase))?.ErrorMessage
            },
            new
            {
                Name = "Google Gemini 2.0 Flash",
                Tier = "Tier 3",
                Status = geminiFailsLastHour >= 3 ? "degraded" : geminiFailsLastHour > 0 ? "warning" : "healthy",
                JobsClassifiedToday = modelCountsToday.FirstOrDefault(m => m.Model.Contains("Gemini", StringComparison.OrdinalIgnoreCase))?.Count ?? 0,
                FailoversToday = events.Count(e => e.FailedProvider.Contains("Gemini", StringComparison.OrdinalIgnoreCase)),
                LastFailureReason = events.FirstOrDefault(e => e.FailedProvider.Contains("Gemini", StringComparison.OrdinalIgnoreCase))?.ErrorMessage
            },
            new
            {
                Name = "Groq Llama 3.1 8B Instant",
                Tier = "Emergency Pass",
                Status = "healthy",
                JobsClassifiedToday = modelCountsToday.FirstOrDefault(m => m.Model.Contains("8B", StringComparison.OrdinalIgnoreCase))?.Count ?? 0,
                FailoversToday = 0,
                LastFailureReason = (string?)null
            },
            new
            {
                Name = "Cosine Vector Fallback",
                Tier = "Tier 5 (Safe Uncategorized)",
                Status = "healthy",
                JobsClassifiedToday = modelCountsToday.FirstOrDefault(m => m.Model.Contains("Cosine", StringComparison.OrdinalIgnoreCase))?.Count ?? 0,
                FailoversToday = 0,
                LastFailureReason = (string?)null
            }
        };

        return Results.Ok(new
        {
            date = targetDate.ToString("yyyy-MM-dd"),
            totalClassifiedToday,
            eventsCount = events.Count,
            providers,
            events
        });
    }

    /// <summary>
    /// GET /api/admin/sectors/dashboard-stats?date=YYYY-MM-DD
    /// Returns 5 Tier metrics (Row 1) and 5 Daily operational metrics (Row 2) for the given date.
    /// </summary>
    private static async Task<IResult> GetDashboardStatsAsync(
        DateTime? date,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var targetDate = date.HasValue ? DateOnly.FromDateTime(date.Value) : DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        // 1. Fetch exact classification records on this date
        var records = await db.JobClassificationRecords
            .AsNoTracking()
            .Where(r => r.ClassifiedAt >= startOfDay && r.ClassifiedAt <= endOfDay)
            .ToListAsync(ct);

        // 2. Fetch failover events on this date
        var events = await db.AiProviderAuditLogs
            .AsNoTracking()
            .Where(e => e.OccurredAt >= startOfDay && e.OccurredAt <= endOfDay)
            .ToListAsync(ct);

        // 3. Count jobs inserted / updated on this date
        var jobsCreated = await db.Jobs.AsNoTracking()
            .CountAsync(j => j.CreatedAt >= startOfDay && j.CreatedAt <= endOfDay, ct);
        var jobsUpdated = await db.Jobs.AsNoTracking()
            .CountAsync(j => j.UpdatedAt >= startOfDay && j.UpdatedAt <= endOfDay, ct);
        var totalInsertedOrSynced = Math.Max(jobsCreated, jobsUpdated);

        // 4. Jobs with non-null sector on this date
        var recordsWithSector = records.Count(r => !string.IsNullOrEmpty(r.SectorSlug));
        var jobsWithSectorToday = await db.Jobs.AsNoTracking()
            .CountAsync(j => j.SectorId != null && ((j.UpdatedAt >= startOfDay && j.UpdatedAt <= endOfDay) || (j.CreatedAt >= startOfDay && j.CreatedAt <= endOfDay)), ct);
        var totalClassified = Math.Max(recordsWithSector, jobsWithSectorToday);

        // Jobs without a sector on this date
        var totalUnclassified = await db.Jobs.AsNoTracking()
            .CountAsync(j => j.SectorId == null && ((j.UpdatedAt >= startOfDay && j.UpdatedAt <= endOfDay) || (j.CreatedAt >= startOfDay && j.CreatedAt <= endOfDay)), ct);

        // Avg confidence
        var confidenceScores = records.Where(r => r.Confidence > 0).Select(r => r.Confidence).ToList();
        var avgConfidence = confidenceScores.Count > 0 ? Math.Round(confidenceScores.Average() * 100, 1) : 95.0;

        // Success rate (%)
        var totalEvaluated = totalClassified + totalUnclassified;
        var successRate = totalEvaluated > 0 ? Math.Round(((double)totalClassified / totalEvaluated) * 100, 1) : 100.0;

        // Helper to count batch runs: group records into batches by 5-second execution windows
        int CountRuns(IEnumerable<JobClassificationRecord> tierRecords) =>
            tierRecords.GroupBy(r => new { r.ClassifiedAt.Hour, r.ClassifiedAt.Minute, Window = r.ClassifiedAt.Second / 5 }).Count();

        // Tier 1: OpenRouter
        var t1Records = records.Where(r => r.ModelUsed.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase) || r.ModelTier.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase)).ToList();
        var t1Fails = events.Where(e => e.FailedProvider.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase)).ToList();
        var t1JobsClassified = t1Records.Count(r => !string.IsNullOrEmpty(r.SectorSlug));
        var t1JobsFailed = t1Fails.Sum(e => e.JobCount);
        var t1SuccessRuns = CountRuns(t1Records);
        var t1FailedRuns = t1Fails.Count;
        var t1Status = t1FailedRuns >= 3 ? "degraded" : t1FailedRuns > 0 ? "warning" : t1JobsClassified > 0 ? "active" : "idle";

        // Tier 2: Groq Llama 3.3 70B
        var t2Records = records.Where(r => (r.ModelUsed.Equals("Groq", StringComparison.OrdinalIgnoreCase) || r.ModelUsed.Contains("Groq Llama 3.3", StringComparison.OrdinalIgnoreCase)) && !r.ModelUsed.Contains("8B", StringComparison.OrdinalIgnoreCase)).ToList();
        var t2Fails = events.Where(e => e.FailedProvider.Contains("Groq", StringComparison.OrdinalIgnoreCase) && !e.AttemptedTier.Contains("8B", StringComparison.OrdinalIgnoreCase)).ToList();
        var t2JobsClassified = t2Records.Count(r => !string.IsNullOrEmpty(r.SectorSlug));
        var t2JobsFailed = t2Fails.Sum(e => e.JobCount);
        var t2SuccessRuns = CountRuns(t2Records);
        var t2FailedRuns = t2Fails.Count;
        var t2Status = t2FailedRuns >= 3 ? "degraded" : t2FailedRuns > 0 ? "warning" : t2JobsClassified > 0 ? "active" : "idle";

        // Tier 3: Google Gemini 2.0 Flash
        var t3Records = records.Where(r => r.ModelUsed.Contains("Gemini", StringComparison.OrdinalIgnoreCase)).ToList();
        var t3Fails = events.Where(e => e.FailedProvider.Contains("Gemini", StringComparison.OrdinalIgnoreCase)).ToList();
        var t3JobsClassified = t3Records.Count(r => !string.IsNullOrEmpty(r.SectorSlug));
        var t3JobsFailed = t3Fails.Sum(e => e.JobCount);
        var t3SuccessRuns = CountRuns(t3Records);
        var t3FailedRuns = t3Fails.Count;
        var t3Status = t3FailedRuns >= 3 ? "degraded" : t3Fails.Count > 0 ? "warning" : t3JobsClassified > 0 ? "active" : "idle";

        // Tier 4: Emergency Pass (Groq Llama 3.1 8B Instant)
        var t4Records = records.Where(r => r.ModelUsed.Contains("8B", StringComparison.OrdinalIgnoreCase)).ToList();
        var t4Fails = events.Where(e => e.AttemptedTier.Contains("8B", StringComparison.OrdinalIgnoreCase)).ToList();
        var t4JobsClassified = t4Records.Count(r => !string.IsNullOrEmpty(r.SectorSlug));
        var t4JobsFailed = t4Fails.Sum(e => e.JobCount);
        var t4SuccessRuns = CountRuns(t4Records);
        var t4FailedRuns = t4Fails.Count;
        var t4Status = t4FailedRuns >= 3 ? "degraded" : t4Fails.Count > 0 ? "warning" : t4JobsClassified > 0 ? "active" : "idle";

        // Tier 5: Safe Uncategorized Fallback
        var t5Fails = events.Where(e => e.FallbackTier.Contains("Safe Uncategorized", StringComparison.OrdinalIgnoreCase)).ToList();
        var t5JobsUnclassified = totalUnclassified;
        var t5SuccessRuns = t5Fails.Count;
        var t5Status = totalUnclassified > 0 ? "active" : "idle";

        var response = new ClassificationDashboardStatsResponse(
            targetDate.ToString("yyyy-MM-dd"),
            new TierMetricDto("Tier 1", "OpenRouter Nemotron 550B", t1JobsClassified, t1JobsFailed, t1SuccessRuns, t1FailedRuns, t1Status),
            new TierMetricDto("Tier 2", "Groq Llama 3.3 70B", t2JobsClassified, t2JobsFailed, t2SuccessRuns, t2FailedRuns, t2Status),
            new TierMetricDto("Tier 3", "Google Gemini 2.0 Flash", t3JobsClassified, t3JobsFailed, t3SuccessRuns, t3FailedRuns, t3Status),
            new TierMetricDto("Emergency Pass", "Groq Llama 3.1 8B Instant", t4JobsClassified, t4JobsFailed, t4SuccessRuns, t4FailedRuns, t4Status),
            new TierMetricDto("Tier 5", "Safe Uncategorized Fallback", 0, t5JobsUnclassified, t5SuccessRuns, 0, t5Status),
            new DailyOperationalMetricsDto(totalClassified, totalUnclassified, avgConfidence, totalInsertedOrSynced, successRate)
        );

        return Results.Ok(response);
    }
}
