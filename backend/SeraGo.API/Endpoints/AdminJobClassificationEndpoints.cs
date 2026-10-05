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
    /// Classify one or more jobs via the decoupled SeraGo-AI service. Returns
    /// per-job results. Best-effort: if the AI service is down or a single job
    /// fails, the rest still classify and the error is surfaced per-job.
    ///
    /// On success the job's sector is set from the SubSector decision (SectorId
    /// + SectorName), the AI trace is stored, a SubSector mapping row is
    /// dedup-inserted, and, when the LLM failed for a job, a local SubSector-based
    /// fallback is attempted before leaving the job uncategorized.
    /// </summary>
    private static async Task<IResult> ClassifyJobsAsync(
        ClassifyJobRequest body,
        ApplicationDbContext db,
        AiClassificationClient aiClient,
        AiClassificationSaveService saveService,
        ILogger<object> logger)
    {
        var jobIds = (
            body.JobId is not null
                ? new List<Guid> { body.JobId.Value }
                : (body.JobIds is null or { Count: 0 }
                    ? throw new ArgumentException("Provide either jobId or jobIds.", nameof(body))
                    : body.JobIds));
        var jobIdArr = jobIds.ToArray();

        if (jobIdArr.Length == 0)
        {
            return Results.Ok(new ClassifyJobsResponse([], 0, 0, "No job ids provided."));
        }

        // Load the jobs (with their current sector) plus ALL active sectors, so
        // the fallback path can match against the accumulated SubSector + Alias
        // vocabulary.
        var jobs = await db.Jobs
            .Include(j => j.Sector)
            .Where(j => jobIdArr.Contains(j.Id))
            .ToListAsync();

        if (jobs.Count == 0)
        {
            return Results.Ok(new ClassifyJobsResponse(
                [],
                0,
                0,
                $"No jobs found for the provided ids ({string.Join(", ", jobIdArr)})."));
        }

        // Build the request the AI service expects: one-to-many jobs with the
        // fields needed for sector classification only. We also send the job's
        // current sector so the AI service can record the 'before' state in the
        // classify log (original_sector_slug/name).
        var requestJobs = new List<AiClassificationRequestJob>(jobs.Count);
        foreach (var j in jobs)
        {
            var currentSlug = j.Sector?.Slug;
            var currentName = j.Sector?.Name;
            var extraOriginal = (currentSlug is not null || currentName is not null)
                ? new Dictionary<string, string>(2)
                { ["sectorSlug"] = currentSlug ?? "", [
                    "sectorName"] = currentName ?? "" }
                : null;

            requestJobs.Add(new AiClassificationRequestJob(
                JobId: j.Id.ToString(),
                Title: j.Title,
                SourceSectors: ParseSourceSectors(j.SourceSectors),
                Description: j.Description,
                ExtraOriginal: extraOriginal));
        }

        // Classify via the decoupled AI service. A failure here (network /
        // timeout / 5xx) is surfaced as an error on every result, not a 500.
        AiClassificationResult[] aiResults;
        string? aiError = null;
        try
        {
            var list = await aiClient.ClassifyAsync(requestJobs);
            aiResults = list.ToArray();

            // Collect any per-job AI-service errors so we can still classify the
            // successful ones and surface the failures per job.
            var aiServiceErrors = aiResults.Where(r => r.Error is not null).ToList();
            if (aiServiceErrors.Count > 0)
            {
                foreach (var r in aiServiceErrors)
                {
                    logger.LogWarning(
                        "SeraGo-AI returned an error for job {JobId}: {Error}",
                        r.JobId, r.Error);
                }
            }
        }
        catch (AiClassificationException ex)
        {
            aiError = ex.Message;
            logger.LogWarning(ex, "SeraGo-AI classify failed for {JobCount} jobs", jobIdArr.Length);

            // Best-effort: when the AI service is unreachable, fall back to the
            // local SubSector matching path for every requested job.
            aiResults = new AiClassificationResult[jobs.Count];
            for (var i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                aiResults[i] = new AiClassificationResult(
                    JobId: j.Id.ToString(),
                    SectorId: null,
                    SectorName: null,
                    SectorSlug: null,
                    SubSectorName: null,
                    Alias: FirstSourceSector(j.SourceSectors),
                    Confidence: null,
                    Reasoning: null,
                    Uncategorized: true,
                    Error: aiError);
            }
        }

        // Apply the results to the tracked jobs: set SectorId/SectorName from
        // the SubSector decision, store the AI trace, dedup-insert SubSector
        // rows, and run the local fallback when the LLM failed.
        var outcomes = await saveService.ApplyAsync(jobs, aiResults);

        var results = new List<ClassificationResultDto>(outcomes.Count);
        var classified = 0;
        var failed = 0;

        var aiById = aiResults.ToDictionary(r => r.JobId, r => r);

        foreach (var outcome in outcomes)
        {
            var job = jobs.First(j => j.Id == Guid.Parse(outcome.JobId));
            aiById.TryGetValue(outcome.JobId, out var ai);

            // Surface the fields that matter: the assigned sector plus the
            // LLM's confidence and reasoning (when the LLM ran). The error
            // (LLM failure / AI service unreachable) is reported separately.
            results.Add(new ClassificationResultDto(
                job.Id,
                outcome.SectorId is not null ? job.Sector?.Slug : null,
                outcome.SectorName,
                ai?.Confidence,
                ai?.Reasoning,
                !outcome.Assigned,
                outcome.LlmError));

            if (outcome.Assigned)
            {
                classified++;
            }
            else
            {
                failed++;
            }
        }

        await db.SaveChangesAsync();

        var classifiedCount = jobs.Count(j => j.SectorId is not null);
        logger.LogInformation(
            "Admin classified {Count} jobs via SeraGo-AI — assigned={Assigned}, uncategorized={Uncategorized}",
            jobIdArr.Length, classifiedCount, jobIdArr.Length - classifiedCount);

        return Results.Ok(new ClassifyJobsResponse(results, classified, failed,
            aiError is not null ? "One or more jobs could not be classified." : null));
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
        List<Guid>? JobIds);

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
        string? Error);

    private static async Task<IResult> GetClassificationLogAsync(
        Guid id,
        ApplicationDbContext db,
        AiClassificationClient aiClient)
    {
        var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return Results.NotFound("Job not found.");
        if (job.ClassificationId == null) return Results.NotFound("No AI classification log found for this job.");

        var log = await aiClient.GetClassificationLogAsync(job.ClassificationId.Value);
        if (log == null) return Results.NotFound("Classification log not found in AI service.");

        return Results.Ok(log);
    }
}
