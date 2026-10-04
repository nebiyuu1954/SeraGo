using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.API.Services;

/// <summary>
/// Applies a batch of AI classification results (from the SeraGo-AI classify
/// API) to tracked Job entities: sets SectorId/SectorName from the sector-label
/// mapping decision, dedup-inserts SectorLabelMapping rows, and, when the
/// LLM failed for a job, runs a local sector-label-based fallback before leaving
/// the job uncategorized.
///
/// The caller loads the jobs, calls the AI service, then hands the outcomes
/// here along with the loaded sectors. One SaveChanges() at the end persists
/// everything atomically.
/// </summary>
public sealed class AiClassificationSaveService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AiClassificationSaveService> _logger;

    public AiClassificationSaveService(
        ApplicationDbContext db,
        ILogger<AiClassificationSaveService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Apply a batch of AI classification outcomes to the loaded jobs.
    /// Returns, per job, whether a sector was assigned and the sector-label alias
    /// that was recorded (if any).
    /// </summary>
    public async Task<IReadOnlyList<ApplyClassificationOutcome>> ApplyAsync(
        IReadOnlyList<Job> jobs,
        IReadOnlyList<AiClassificationResult> results,
        CancellationToken ct = default)
    {
        var outcomes = new List<ApplyClassificationOutcome>(jobs.Count);
        var newlyAdded = new HashSet<(Guid SectorId, string Alias)>();
        var byId = jobs.ToDictionary(j => j.Id.ToString(), j => j);

        foreach (var result in results)
        {
            if (byId.TryGetValue(result.JobId, out var job))
            {
                outcomes.Add(await ApplyOneAsync(job, result, newlyAdded, ct));
            }
            else
            {
                _logger.LogWarning(
                    "Classification result for unknown job {JobId} — skipping",
                    result.JobId);
                outcomes.Add(ApplyClassificationOutcome.Skipped(jobId: result.JobId));
            }
        }

        return outcomes;
    }

    private async Task<ApplyClassificationOutcome> ApplyOneAsync(
        Job job,
        AiClassificationResult result,
        HashSet<(Guid SectorId, string Alias)> newlyAdded,
        CancellationToken ct)
    {
        // ── LLM classified successfully ────────────────────────────────────
        if (!result.Uncategorized && result.SectorId is not null)
        {
            var sectorId = Guid.Parse(result.SectorId);
            var sectorName = result.SubSectorName ?? result.SectorName ?? "";

            job.SectorId = sectorId;
            job.SectorName = sectorName;
            job.ClassificationId = result.LogId;
            // Note: we do NOT set AiClassification / AiClassifiedAt / UpdatedAt here.
            // The classify trace now lives in the Django AiClassificationLog table
            // (pointed to by ClassificationId), and UpdatedAt is reserved for real
            // job-data changes (scraper updates), not for classification.

            // Dedup-insert one SectorLabelMapping row for (SectorId, Alias) where
            // Alias is the job's original source sector text and SectorName is the
            // canonical sector name the AI stored.
            var alias = !string.IsNullOrWhiteSpace(result.Alias)
                ? result.Alias
                : (!string.IsNullOrWhiteSpace(job.SourceSectors)
                    ? FirstSourceSector(job.SourceSectors)
                    : null);

            var sectorLabelRecorded = await EnsureSectorLabelMappingRowAsync(
                sectorId, sectorName, alias, job.Id, newlyAdded, ct);

            return ApplyClassificationOutcome.Success(
                jobId: job.Id.ToString(),
                sectorId: sectorId,
                sectorName: sectorName,
                sectorLabelRecorded: sectorLabelRecorded,
                sectorLabelAlias: alias,
                fallback: false);
        }

        // ── LLM returned uncategorized OR failed ──────────────────────────
        // Run the local sector-label fallback before giving up.
        var fallback = await TryFallbackAsync(job, ct);
        if (fallback.Assigned)
        {
            job.SectorId = fallback.SectorId;
            job.SectorName = fallback.SectorName;
            // Fallback path: no Groq log was created (the LLM didn't run), so there's
            // no LogId to store. ClassificationId stays null for fallback-classified jobs.

            var alias = !string.IsNullOrWhiteSpace(result.Alias)
                ? result.Alias
                : (!string.IsNullOrWhiteSpace(job.SourceSectors)
                    ? FirstSourceSector(job.SourceSectors)
                    : null);

            await EnsureSectorLabelMappingRowAsync(
                fallback.SectorId, fallback.SectorName ?? "", alias, job.Id, newlyAdded, ct);

            return ApplyClassificationOutcome.Success(
                jobId: job.Id.ToString(),
                sectorId: fallback.SectorId!.Value,
                sectorName: fallback.SectorName ?? "",
                sectorLabelRecorded: true,
                sectorLabelAlias: alias,
                fallback: true);
        }

        // Nothing found — leave the job's sector alone, no mapping row.
        _logger.LogDebug(
            "Job {JobId} left uncategorized (LLM={LlmUncat}, fallback={FallbackHit})",
            job.Id, result.Uncategorized, fallback.Assigned);
        return ApplyClassificationOutcome.Uncategorized(
            jobId: job.Id.ToString(),
            llmUncategorized: result.Uncategorized,
            llmError: result.Error);
    }

    // ── SectorLabelMapping dedup insert ──────────────────────────────────

    /// <summary>
    /// Insert one SectorLabelMapping row for (SectorId, Alias) if it does not
    /// already exist. Returns true when a new row was inserted, false when it
    /// already existed.
    /// </summary>
    public async Task<bool> EnsureSectorLabelMappingRowAsync(
        Guid? sectorId,
        string sectorName,
        string? alias,
        Guid jobId,
        HashSet<(Guid SectorId, string Alias)> newlyAdded,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return false;
        }

        var normalizedAlias = SectorSeedData.Normalize(alias);
        if (normalizedAlias.Length == 0)
        {
            return false;
        }

        if (sectorId.HasValue && newlyAdded.Contains((sectorId.Value, normalizedAlias)))
        {
            return false;
        }

        // Fast check via the unique (SectorId, Alias) index.
        var existing = await _db.SectorLabelMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SectorId == sectorId && s.Alias == normalizedAlias, ct);

        if (existing is not null)
        {
            return false;
        }

        if (sectorId.HasValue)
        {
            newlyAdded.Add((sectorId.Value, normalizedAlias));
        }

        _db.SectorLabelMappings.Add(new SectorLabelMapping
        {
            Id = Guid.NewGuid(),
            SectorId = sectorId!.Value,
            SectorName = sectorName,
            Alias = normalizedAlias,
        });

        _logger.LogDebug(
            "Inserted SectorLabelMapping (SectorId={SectorId}, Alias={Alias}) from job {JobId}",
            sectorId, normalizedAlias, jobId);

        return true;
    }

    // ── Local fallback when the LLM fails ───────────────────────────────

    /// <summary>
    /// When the LLM returned uncategorized or failed for a job, try to assign
    /// a sector by matching the job's title + source sector text against the
    /// accumulated SectorLabelMapping rows (and, as a last resort, the static
    /// SectorAlias vocabulary).
    ///
    /// Returns the assigned sector (if any) and the sector-label alias that would
    /// be recorded.
    /// </summary>
    public async Task<FallbackResult> TryFallbackAsync(Job job, CancellationToken ct)
    {
        var rawSector = !string.IsNullOrWhiteSpace(job.SourceSectors)
            ? FirstSourceSector(job.SourceSectors)
            : null;
        var title = job.Title;

        var sectors = await _db.Sectors
            .Where(s => s.IsActive)
            .Include(s => s.Aliases)
            .AsNoTracking()
            .ToListAsync(ct);

        // 1. Match the job's source sector text against the observed
        //    SectorLabelMapping aliases for each canonical sector (best signal —
        //    real AI-derived mappings), then fall back to the static
        //    SectorAlias vocabulary.
        Sector? bestSector = null;
        int bestScore = 0;
        string? bestAlias = null;

        var labelMappingsBySector = await _db.SectorLabelMappings
            .AsNoTracking()
            .Where(s => s.Sector.IsActive)
            .Include(s => s.Sector)
            .ToListAsync(ct);

        var labelAliasesBySector = labelMappingsBySector
            .GroupBy(s => s.SectorId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => s.Alias).ToHashSet(StringComparer.Ordinal));

        foreach (var sector in sectors)
        {
            var score = ScoreMatch(sector.Id, labelAliasesBySector, sector, rawSector, title);
            if (score > bestScore)
            {
                bestScore = score;
                bestSector = sector;
                bestAlias = rawSector;
            }
        }

        // Threshold: require at least a partial match to avoid spurious
        // assignments. A score of 2+ means we matched a meaningful token or an
        // exact alias, which is enough to trust the assignment.
        if (bestScore < 2 || bestSector is null)
        {
            return FallbackResult.None;
        }

        return FallbackResult.Success(
            bestSector.Id,
            bestSector.Name,
            bestAlias);
    }

    /// <summary>
    /// Simple overlap score between the job and one canonical sector.
    /// Higher = better. Uses the observed SectorLabelMapping aliases for the
    /// sector first, then the static SectorAlias vocabulary.
    /// </summary>
    private static int ScoreMatch(
        Guid sectorId,
        IReadOnlyDictionary<Guid, HashSet<string>> labelAliasesBySector,
        Sector sector,
        string? rawSector,
        string? title)
    {
        var score = 0;

        // 1. Observed SectorLabelMapping aliases for this sector (real AI-derived).
        if (rawSector is not null
            && labelAliasesBySector.TryGetValue(sectorId, out var labelAliases))
        {
            foreach (var alias in labelAliases)
            {
                if (alias == rawSector.ToLowerInvariant())
                {
                    score += 5; // exact observed mapping
                }
                else if (alias.Contains(rawSector.ToLowerInvariant())
                         || rawSector.ToLowerInvariant().Contains(alias))
                {
                    score += 3; // strong overlap
                }
            }
        }

        // 2. Static SectorAlias vocabulary for this sector (admin-maintained).
        if (rawSector is not null)
        {
            var normalized = SectorSeedData.Normalize(rawSector);
            if (sector.Aliases.Any(a => a.Alias == normalized))
            {
                score += 4; // exact static alias match
            }
        }

        // 3. Title keyword overlap — only as a weak signal unless a real alias
        //    also matched. Reuses the existing keyword rules so we don't invent
        //    a new vocabulary here.
        if (!string.IsNullOrWhiteSpace(title))
        {
            var slug = SectorNormalizer.ClassifyTitle(title);
            if (slug == sector.Slug)
            {
                score += 1; // title keyword suggests this sector
            }
        }

        return score;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static string? FirstSourceSector(string? sourceSectors)
    {
        if (string.IsNullOrWhiteSpace(sourceSectors))
        {
            return null;
        }
        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(sourceSectors);
            return parsed is not null && parsed.Count > 0 ? parsed[0] : null;
        }
        catch
        {
            return null;
        }
    }
}

// ── Outcomes ───────────────────────────────────────────────────────────

public sealed record ApplyClassificationOutcome(
    string JobId,
    bool Assigned,
    bool Fallback,
    bool SectorLabelRecorded,
    Guid? SectorId,
    string? SectorName,
    string? SectorLabelAlias,
    bool LlmUncategorized,
    string? LlmError)
{
    public static ApplyClassificationOutcome Success(
        string jobId,
        Guid sectorId,
        string sectorName,
        bool sectorLabelRecorded,
        string? sectorLabelAlias,
        bool fallback) =>
        new(jobId, true, fallback, sectorLabelRecorded, sectorId, sectorName, sectorLabelAlias, false, null);

    public static ApplyClassificationOutcome Uncategorized(
        string jobId,
        bool llmUncategorized,
        string? llmError) =>
        new(jobId, false, false, false, null, null, null, llmUncategorized, llmError);

    public static ApplyClassificationOutcome Skipped(string jobId) =>
        new(jobId, false, false, false, null, null, null, false, "job not found");
}

public sealed record FallbackResult(
    bool Assigned,
    Guid? SectorId,
    string? SectorName,
    string? Alias)
{
    public static FallbackResult None => new(false, null, null, null);
    public static FallbackResult Success(Guid? sectorId, string sectorName, string? alias) =>
        new(true, sectorId, sectorName, alias);
}

public static class AiClassificationSaveServiceExtensions
{
    public static async Task<IReadOnlyList<ApplyClassificationOutcome>> ApplyAsync(
        this AiClassificationSaveService service,
        IReadOnlyList<Job> jobs,
        IReadOnlyList<AiClassificationResult> results,
        CancellationToken ct = default) =>
        await service.ApplyAsync(jobs, results, ct);
}
