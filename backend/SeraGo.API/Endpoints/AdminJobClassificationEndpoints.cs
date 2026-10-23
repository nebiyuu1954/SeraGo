using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Services;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Admin-only AI job classification endpoints. Behind the ADMIN_API_ENABLED gate.
///
/// POST /api/admin/jobs/classify — classify one or more jobs via the decoupled
/// SeraGo-AI service (POST /api/ai/classify).
///   Body: { "jobId": guid }                → single job
///          { "jobIds": [guid, ...] }       → batch
///
///   Returns: { results: [ { jobId, sectorSlug, sectorName, confidence,
///             reasoning, uncategorized, error? }, ... ],
///             classified: int, failed: int, message?: string }
///
/// Roles: Admin only. Auth: JWT Bearer (same pipeline as every other endpoint).
///
/// On success the job's sector is set from the SubSector decision (SectorId +
/// SectorName), the AI trace is stored, a SubSector mapping row is dedup-inserted,
/// and, when the LLM failed for a job, a local SubSector-based fallback is
/// attempted before leaving the job uncategorized.
///
/// Best-effort: if the AI service is down or a single job fails, the rest still
/// classify and the error is surfaced per-job (not a 500).
/// </summary>
public static class AdminJobClassificationEndpoints
{
    public static IEndpointRouteBuilder MapAdminJobClassificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/jobs")
            .WithTags("Jobs (admin — AI classification)")
            .RequireAuthorization("Admin");

        group.MapPost("/classify", ClassifyJobsAsync).WithOpenApi();
        group.MapGet("/{id:guid}/classification-log", GetClassificationLogAsync).WithOpenApi();

        return app;
    }

    /// <summary>
    /// Classify one or more jobs via the 4-tier unified classification service
    /// (Tier 1: Gemini -> Tier 2: Groq -> Tier 3: Ollama -> Tier 4: Cosine Similarity).
    /// </summary>
    private static async Task<IResult> ClassifyJobsAsync(
        ClassifyJobRequest body,
        ApplicationDbContext db,
        UnifiedJobClassificationService unifiedClassifier,
        ILogger<object> logger,
        CancellationToken ct)
    {
        List<Job> jobs;

        if (!string.IsNullOrWhiteSpace(body.Date))
        {
            DateTimeOffset startOfDay;
            DateTimeOffset endOfDay;

            if (body.Date.Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                startOfDay = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero);
                endOfDay = new DateTimeOffset(today.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc), TimeSpan.Zero);
            }
            else if (DateOnly.TryParse(body.Date, out var targetDate))
            {
                startOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero);
                endOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc), TimeSpan.Zero);
            }
            else
            {
                return Results.BadRequest(new ClassifyJobsResponse(0, 0, "Invalid date format. Expected YYYY-MM-DD or 'today'."));
            }

            var dateQuery = db.Jobs.Include(j => j.Sector).Where(j =>
                (j.UpdatedAt >= startOfDay && j.UpdatedAt <= endOfDay) ||
                (j.CreatedAt >= startOfDay && j.CreatedAt <= endOfDay) ||
                (j.PublishedAt.HasValue && j.PublishedAt.Value >= startOfDay && j.PublishedAt.Value <= endOfDay));

            if (body.Limit.HasValue && body.Limit > 0)
            {
                dateQuery = dateQuery.Take(body.Limit.Value);
            }

            jobs = await dateQuery.ToListAsync(ct);
        }
        else
        {
            var jobIds = (
                body.JobId is not null
                    ? new List<Guid> { body.JobId.Value }
                    : (body.JobIds is null or { Count: 0 }
                        ? throw new ArgumentException("Provide either jobId, jobIds, or date.", nameof(body))
                        : body.JobIds));
            var jobIdArr = jobIds.ToArray();

            if (jobIdArr.Length == 0)
            {
                return Results.Ok(new ClassifyJobsResponse([], 0, 0, "No job ids provided."));
            }

            jobs = await db.Jobs
                .Include(j => j.Sector)
                .Where(j => jobIdArr.Contains(j.Id))
                .ToListAsync(ct);
        }

        if (jobs.Count == 0)
        {
            return Results.Ok(new ClassifyJobsResponse(
                [],
                0,
                0,
                "No jobs found for the specified criteria."));
        }

        var outcomes = await unifiedClassifier.ClassifyAndSaveAsync(jobs, ct);

        var results = new List<ClassificationResultDto>(outcomes.Count);
        var classified = 0;
        var failed = 0;

        foreach (var outcome in outcomes)
        {
            var isAssigned = outcome.SectorId.HasValue;

            results.Add(new ClassificationResultDto(
                outcome.JobId,
                outcome.SectorSlug,
                outcome.SectorName,
                outcome.Confidence,
                outcome.Reasoning,
                !isAssigned,
                outcome.Error,
                null));

            if (isAssigned)
            {
                classified++;
            }
            else
            {
                failed++;
            }
        }

        logger.LogInformation(
            "Admin classified {Count} jobs via Unified Pipeline — assigned={Assigned}, uncategorized={Uncategorized}",
            jobs.Count, classified, failed);

        return Results.Ok(new ClassifyJobsResponse(results, classified, failed));
    }

    /// <summary>Parse the job's JSON-array sourceSectors into a list, or empty.</summary>
    private static List<string> ParseSourceSectors(string? sourceSectors)
    {
        if (string.IsNullOrWhiteSpace(sourceSectors))
        {
            return [];
        }
        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(sourceSectors);
            return parsed ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? FirstSourceSector(string? sourceSectors)
    {
        var list = ParseSourceSectors(sourceSectors);
        return list.Count > 0 ? list[0] : null;
    }

    // ── DTOs ──────────────────────────────────────────────────────────

    public sealed record ClassifyJobRequest(
        Guid? JobId,
        List<Guid>? JobIds,
        string? Date = null,
        int? Limit = null);

    public sealed record ClassifyJobsResponse(
        List<ClassificationResultDto> Results,
        int Classified,
        int Failed,
        string? Message = null)
    {
        // Convenience factory for the "no results" informational responses.
        public ClassifyJobsResponse(int Classified, int Failed, string? Message)
            : this([], Classified, Failed, Message) { }
    }

    public sealed record ClassificationResultDto(
        Guid JobId,
        string? SectorSlug,
        string? SectorName,
        double? Confidence,
        string? Reasoning,
        bool Uncategorized,
        string? Error,
        List<SuggestedSectorDto>? SuggestedSectors = null);

    public sealed record SuggestedSectorDto(
        string SectorId,
        string SectorName,
        string SectorSlug);

    private static async Task<IResult> GetClassificationLogAsync(
        Guid id,
        ApplicationDbContext db,
        AiClassificationClient aiClient)
    {
        var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return Results.NotFound("Job not found.");
        if (job.ClassificationId == null) return Results.NotFound("No legacy AI classification log found for this job.");

        try
        {
            var log = await aiClient.GetClassificationLogAsync(job.ClassificationId.Value);
            if (log == null) return Results.NotFound("Classification log not found in AI service.");
            return Results.Ok(log);
        }
        catch (Exception)
        {
            return Results.NotFound("Legacy AI service classification log unavailable.");
        }
    }
}
