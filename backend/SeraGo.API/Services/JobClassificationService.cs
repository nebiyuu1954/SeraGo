using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Services;

/// <summary>
/// Classifies a job into a canonical SeraGo sector (and optionally experience
/// level / job type / work mode) using Groq. The model is given the REAL list
/// of active sectors from the DB so it picks from the actual vocabulary rather
/// than inventing one. The raw AI response JSON is stored on the Job as
/// <c>AiClassification</c> — the source of truth for what was decided and why.
///
/// English canonical sectors only: the prompt lists sector slugs + names in
/// English and the response is parsed into the canonical <see cref="Sector"/>.
/// </summary>
public sealed class JobClassificationService
{
    private readonly GroqClient _groq;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<JobClassificationService> _logger;

    public JobClassificationService(
        GroqClient groq,
        IServiceScopeFactory scopeFactory,
        ApplicationDbContext db,
        ILogger<JobClassificationService> logger)
    {
        _groq = groq;
        _scopeFactory = scopeFactory;
        _db = db;
        _logger = logger;
    }

    // ── Prompt parts (assembled per-job) ─────────────────────────────

    /// <summary>System prompt — fixed, lists the real canonical sectors from the DB.</summary>
    private static string SystemPrompt(SectorVocabulary vocabulary) =>
        $$"""
        {{CategoricalPrompt(vocabulary)}}

        You are a job-classification expert. Classify each job into ONE canonical
        sector from the list above. You MUST choose a sectorSlug from that exact
        list — do not invent a sector that isn't listed.

        Rules:
        - Pick the SINGLE best sector for the job.
        - If the job clearly doesn't fit any listed sector, set
          "uncategorized": true and leave sectorSlug/sectorName empty. NEVER
          invent a sector.
        - Normalize experienceLevel to one of: Entry, Junior, Mid, Senior, Lead,
          or leave it null if the job doesn't state a level.
        - Normalize jobType to one of: FullTime, PartTime, Contract, Internship,
          Freelance, Temporary, Remote, Other — or null.
        - Normalize workMode to one of: Onsite, Remote, Hybrid — or null.
        - confidence is a number 0.0–1.0 (your estimate of how sure you are).
        - reasoning is a short English sentence explaining why.
        - Respond with ONLY a JSON object matching the schema below. No markdown,
          no text outside the JSON.

        JSON schema (return exactly these fields):
        {
          "sectorSlug": "technology-it",     // slug from the list above, or null
          "sectorName": "Technology & IT",    // the name matching that slug, or null
          "experienceLevel": "Mid",           // Entry/Junior/Mid/Senior/Lead or null
          "jobType": "FullTime",             // FullTime/PartTime/Contract/Internship/
                                             //   Freelance/Temporary/Remote/Other or null
          "workMode": "Remote",              // Onsite/Remote/Hybrid or null
          "confidence": 0.92,                 // number 0.0-1.0
          "reasoning": "Software engineering role in a tech company",
          "uncategorized": false             // true ONLY when no sector fits
        }
        """;

    /// <summary>User prompt — the job's text fields.</summary>
    private static string UserPrompt(Job job, SectorVocabulary vocabulary)
    {
        var sourceHint = !string.IsNullOrWhiteSpace(job.SourceSectors)
            ? $"Source sectors (raw): {job.SourceSectors}\n"
            : "";

        return $$"""
        Classify this job into exactly one sector from the list above.

        Title: {{job.Title}}
        Company: {{job.Company}}
        Location: {{job.Location}}
        {{sourceHint}}Description:
        {{job.Description}}

        Additional hints:
        - Experience level hint: {{job.ExperienceLevel ?? "not specified"}}
        - Job type hint: {{job.JobType}}
        - Work mode hint: {{job.WorkMode}}
        - Skills hint: {{job.Skills ?? "not specified"}}

        Return a JSON object with: sectorSlug, sectorName, experienceLevel,
        jobType, workMode, confidence, reasoning, uncategorized.
        """;
    }

    /// <summary>Categorical prompt listing the real active sectors.</summary>
    private static string CategoricalPrompt(SectorVocabulary vocabulary)
    {
        var lines = new List<string>
        {
            "LOW CATEGORICAL TABLE — choose ONE slug from below:",
        };
        foreach (var s in vocabulary.Sectors)
        {
            lines.Add($"  {s.Slug}  {s.Name}");
        }

        if (vocabulary.UncategorizedName is not null)
        {
            lines.Add(
                $"  (if none of the above fit the job, set \"uncategorized\": true " +
                $"and leave sectorSlug/sectorName null — do NOT invent a sector slug)");
        }

        return string.Join("\n", lines);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// Classify one job via Groq (32B). Returns the parsed classification or
    /// null when the AI service is unavailable / failed. The job is NOT written
    /// here — the caller decides whether to commit the result.
    /// </summary>
    public async Task<ClassificationResult?> ClassifyAsync(
        Job job, CancellationToken ct = default)
    {
        var vocabulary = await LoadVocabularyAsync(ct);
        var system = SystemPrompt(vocabulary);
        var user = UserPrompt(job, vocabulary);

        try
        {
            var raw = await _groq.ChatJsonAsync<ClassificationRequest>(
                "openai/gpt-oss-120b", system, user, timeoutSeconds: 60, ct);

            // Validate the sector slug against the real vocabulary.
            if (!string.IsNullOrWhiteSpace(raw.SectorSlug)
                && !vocabulary.SlugSet.Contains(raw.SectorSlug))
            {
                _logger.LogWarning(
                    "Job {JobId} classification returned unknown sectorSlug={Slug} — treating as uncategorized",
                    job.Id, raw.SectorSlug);
                raw = raw with { SectorSlug = null, SectorName = null, Uncategorized = true };
            }

            return raw.ToResult();
        }
        catch (AiServiceException ex)
        {
            _logger.LogWarning(ex, "Failed to classify job {JobId} via Groq", job.Id);
            return null;
        }
    }

    /// <summary>
    /// Classify a batch of jobs (fire-and-forget style — per-job failures are
    /// logged and skipped, not propagated). Each result includes the job id so
    /// the caller can commit them. Slow jobs don't block the batch.
    /// </summary>
    public async Task<IReadOnlyList<ClassificationOutcome>> ClassifyBatchAsync(
        IReadOnlyList<Job> jobs, CancellationToken ct = default)
    {
        var vocabulary = await LoadVocabularyAsync(ct);
        var system = SystemPrompt(vocabulary);
        var outcomes = new List<ClassificationOutcome>(jobs.Count);

        foreach (var job in jobs)
        {
            if (ct.IsCancellationRequested) break;

            var user = UserPrompt(job, vocabulary);
            try
            {
                var raw = await _groq.ChatJsonAsync<ClassificationRequest>(
                    "openai/gpt-oss-120b", system, user, timeoutSeconds: 60, ct);

                // Validate sector slug.
                if (!string.IsNullOrWhiteSpace(raw.SectorSlug)
                    && !vocabulary.SlugSet.Contains(raw.SectorSlug))
                {
                    _logger.LogWarning(
                        "Job {JobId} classification returned unknown sectorSlug={Slug} — treating as uncategorized",
                        job.Id, raw.SectorSlug);
                    raw = raw with { SectorSlug = null, SectorName = null, Uncategorized = true };
                }

                outcomes.Add(new ClassificationOutcome(job.Id, raw.ToResult(), null));
            }
            catch (AiServiceException ex)
            {
                _logger.LogWarning(ex, "Failed to classify job {JobId} via Groq", job.Id);
                outcomes.Add(new ClassificationOutcome(job.Id, null, ex.Message));
            }
        }

        return outcomes;
    }

    /// <summary>
    /// Legacy helper — applies a classification result to a tracked Job entity.
    /// This service is kept only for backwards compatibility; the active classify
    /// flow uses AiClassificationClient → the Django AI service instead.
    /// 
    /// Note: the old AiClassification / AiClassifiedAt columns on Job were removed
    /// in favor of the Django-side AiClassificationLog table + Jobs.ClassificationId.
    /// This method no longer writes those columns.
    /// </summary>
    public (string? SectorSlug, string? SectorName) ApplyClassification(
        Job job, ClassificationResult? result)
    {
        if (result is null)
        {
            return (null, null);
        }

        var (slug, name) = (!string.IsNullOrWhiteSpace(result.SectorSlug))
            ? (result.SectorSlug, result.SectorName)
            : (null, null);

        if (slug is not null)
        {
            job.SectorName = name;
        }

        return (slug, name);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private async Task<SectorVocabulary> LoadVocabularyAsync(CancellationToken ct)
    {
        var sectors = await _db.Sectors
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Slug })
            .ToListAsync(ct);

        var slugSet = sectors.Select(s => s.Slug).ToHashSet(StringComparer.Ordinal);
        var uncategorized = sectors.FirstOrDefault(s => s.Slug == "uncategorized");

        var sortedSectors = sectors.Select(s => (s.Slug, s.Name)).ToList();
        sortedSectors.Sort((a, b) => string.Compare(a.Slug, b.Slug, StringComparison.Ordinal));

        return new SectorVocabulary(
            sortedSectors,
            slugSet,
            uncategorized?.Name ?? "Uncategorized");
    }
}

// ── Contracts ────────────────────────────────────────────────────────

/// <summary>The vocabulary the AI must choose from — loaded from the real DB.</summary>
public sealed record SectorVocabulary(
    IReadOnlyList<(string Slug, string Name)> Sectors,
    HashSet<string> SlugSet,
    string UncategorizedName);

/// <summary>The AI's classification of one job — maps to the JSON we store.</summary>
public sealed record ClassificationResult(
    string? SectorSlug,
    string? SectorName,
    string? ExperienceLevel,
    string? JobType,
    string? WorkMode,
    double Confidence,
    string Reasoning,
    bool Uncategorized)
{
    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

/// <summary>The JSON the AI returns. Same shape as ClassificationResult.</summary>
public sealed record ClassificationRequest(
    [property: JsonPropertyName("sectorSlug")] string? SectorSlug,
    [property: JsonPropertyName("sectorName")] string? SectorName,
    [property: JsonPropertyName("experienceLevel")] string? ExperienceLevel,
    [property: JsonPropertyName("jobType")] string? JobType,
    [property: JsonPropertyName("workMode")] string? WorkMode,
    [property: JsonPropertyName("confidence")] double Confidence,
    [property: JsonPropertyName("reasoning")] string Reasoning,
    [property: JsonPropertyName("uncategorized")] bool Uncategorized)
{
    public static readonly JsonSerializerOptions JsonOpts = ClassificationResult.JsonOpts;

    public ClassificationResult ToResult() =>
        new(SectorSlug, SectorName, ExperienceLevel, JobType, WorkMode, Confidence, Reasoning, Uncategorized);
}

/// <summary>Outcome of classifying one job in a batch — success or failure.</summary>
public sealed record ClassificationOutcome(
    Guid JobId,
    ClassificationResult? Result,
    string? Error);
