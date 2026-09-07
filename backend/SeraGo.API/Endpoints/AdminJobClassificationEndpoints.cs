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
/// POST /api/admin/jobs/classify — classify one or more jobs via Groq (32B).
///   Body: { "jobId": guid }                → single job
///          { "jobIds": [guid, ...] }       → batch
///
///   Returns: { results: [ { jobId, sectorSlug, sectorName, experienceLevel,
///             jobType, workMode, confidence, reasoning, uncategorized,
///             error? }, ... ], classified: int, failed: int, message?: string }
///
/// Roles: Admin only. Auth: JWT Bearer (same pipeline as every other endpoint).
///
/// The classification service writes the raw AI JSON (source of truth) to each
/// Job's AiClassification field, sets AiClassifiedAt, and updates SectorName
/// when the model picks a canonical sector. AiClassifiedAt lets you re-classify
/// later and know what was old.
///
/// Best-effort: if Groq is down or a single job fails, the rest still classify
/// and the error is surfaced per-job (not a 500).
/// </summary>
public static class AdminJobClassificationEndpoints
{
    public static IEndpointRouteBuilder MapAdminJobClassificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/jobs")
            .WithTags("Jobs (admin — AI classification)")
            .RequireAuthorization("Admin");

        group.MapPost("/classify", ClassifyJobsAsync).WithOpenApi();

        return app;
    }

    /// <summary>
    /// Classify one or more jobs via Groq (32B). Returns per-job results with
    /// the AI's chosen sector (validated against the real DB vocabulary) and any
    /// normalized experience level / job type / work mode.
    ///
    /// Best-effort: if Groq is down or a single job fails, the rest still
    /// classify and the error is surfaced per-job (not a 500).
    /// </summary>
    private static async Task<IResult> ClassifyJobsAsync(
        ClassifyJobRequest body,
        ApplicationDbContext db,
        JobClassificationService classifier,
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

        // Load the jobs (with their sectors) plus ALL active sectors so we
        // can resolve the AI's chosen slug to a real Sector row.
        var jobs = await db.Jobs
            .Include(j => j.Sector)
            .Where(j => jobIdArr.Contains(j.Id))
            .ToListAsync();

        var allSectorsBySlug = await db.Sectors
            .Where(s => s.IsActive)
            .ToDictionaryAsync(s => s.Slug, s => s, StringComparer.Ordinal);

        if (jobs.Count == 0)
        {
            return Results.Ok(new ClassifyJobsResponse(
                [],
                0,
                0,
                $"No jobs found for the provided ids ({string.Join(", ", jobIdArr)})."));
        }

        // Classify — per-job failures are logged and skipped, not propagated.
        var outcomes = await classifier.ClassifyBatchAsync(jobs);

        var results = new List<ClassificationResultDto>(outcomes.Count);
        var classified = 0;
        var failed = 0;

        foreach (var outcome in outcomes)
        {
            var job = jobs.First(j => j.Id == outcome.JobId);

            if (outcome.Result is not null)
            {
                var (slug, name, json, classifiedAt) = classifier.ApplyClassification(job, outcome.Result);

                // Resolve SectorId by slug from the sectors already loaded with
                // the jobs (the Include(j => j.Sector) brings each job's sector
                // into memory). Build a slug → sector map from the loaded sectors.
                if (slug is not null)
                {
                    var sector = allSectorsBySlug.GetValueOrDefault(slug);
                    if (sector is not null)
                    {
                        job.SectorId = sector.Id;
                        job.SectorName = name;
                    }
                    else
                    {
                        // Should be impossible — we validate against the live
                        // vocabulary. Defensive: don't apply a stale slug.
                        logger.LogWarning(
                            "Job {JobId} AI picked slug {Slug} but no matching Sector row loaded — leaving uncategorized",
                            job.Id, slug);
                        slug = null;
                        name = null;
                    }
                }

                if (slug is null)
                {
                    // AI said "uncategorized" or validation rejected the slug —
                    // do NOT overwrite an existing SectorId/SectorName with null
                    // (preserves a prior human classification).
                }

                job.AiClassification = json;
                job.AiClassifiedAt = classifiedAt;
                job.UpdatedAt = DateTimeOffset.UtcNow;
                classified++;
            }
            else
            {
                failed++;
            }

            results.Add(new ClassificationResultDto(
                job.Id,
                outcome.Result?.SectorSlug,
                outcome.Result?.SectorName,
                outcome.Result?.ExperienceLevel,
                outcome.Result?.JobType,
                outcome.Result?.WorkMode,
                outcome.Result?.Confidence,
                outcome.Result?.Reasoning,
                outcome.Result?.Uncategorized ?? false,
                outcome.Error));

    
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Admin classified {Count} jobs via Groq — classified={Classified}, failed={Failed}",
            jobIdArr.Length, classified, failed);

        return Results.Ok(new ClassifyJobsResponse(results, classified, failed));
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
        string? ExperienceLevel,
        string? JobType,
        string? WorkMode,
        double? Confidence,
        string? Reasoning,
        bool Uncategorized,
        string? Error);
}
