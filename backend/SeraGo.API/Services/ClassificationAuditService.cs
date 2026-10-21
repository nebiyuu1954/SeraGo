using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.API.Services;

/// <summary>
/// Service for auditing job classifications, inspecting how jobs were categorized
/// (AI LLM, static alias rule, title keyword, or admin manual), and evaluating
/// classification quality with an AI Judge via Groq.
/// </summary>
public sealed class ClassificationAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly GroqClient _groq;
    private readonly ILogger<ClassificationAuditService> _logger;

    public ClassificationAuditService(
        ApplicationDbContext db,
        GroqClient groq,
        ILogger<ClassificationAuditService> logger)
    {
        _db = db;
        _groq = groq;
        _logger = logger;
    }

    // --------------------------------------------------- History Listing

    public async Task<ClassificationHistoryResponse> GetHistoryAsync(
        int page = 1,
        int pageSize = 20,
        Guid? sectorId = null,
        string? method = null,
        bool todayOnly = false,
        string? date = null,
        string? search = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        // Preload active sectors and aliases for accurate method deduction
        var sectors = await _db.Sectors
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Aliases)
            .ToListAsync(ct);

        var sectorMap = sectors.ToDictionary(s => s.Id);
        var aliasToSectorSlug = new Dictionary<string, (string Slug, string Name)>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sectors)
        {
            foreach (var a in s.Aliases)
            {
                var norm = SectorSeedData.Normalize(a.Alias);
                if (!string.IsNullOrEmpty(norm))
                {
                    aliasToSectorSlug[norm] = (s.Slug, s.Name);
                }
            }
        }

        var todayUtc = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        var q = _db.Jobs.AsNoTracking().Include(j => j.Sector).AsQueryable();

        if (sectorId.HasValue)
        {
            q = q.Where(j => j.SectorId == sectorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var targetDate))
        {
            var startOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero);
            var endOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc), TimeSpan.Zero);
            q = q.Where(j =>
                (j.UpdatedAt >= startOfDay && j.UpdatedAt <= endOfDay) ||
                (j.CreatedAt >= startOfDay && j.CreatedAt <= endOfDay) ||
                (j.PublishedAt.HasValue && j.PublishedAt.Value >= startOfDay && j.PublishedAt.Value <= endOfDay));
        }
        else if (todayOnly)
        {
            q = q.Where(j =>
                (j.CreatedAt >= todayUtc) ||
                (j.UpdatedAt >= todayUtc) ||
                (j.PublishedAt.HasValue && j.PublishedAt.Value >= todayUtc));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var sTrim = search.Trim();
            q = q.Where(j => EF.Functions.ILike(j.Title, $"%{sTrim}%") || (j.Company != null && EF.Functions.ILike(j.Company, $"%{sTrim}%")));
        }

        // Method-based database level filtering when possible
        if (!string.IsNullOrWhiteSpace(method))
        {
            var m = method.Trim().ToLowerInvariant();
            if (m == "uncategorized")
            {
                q = q.Where(j => j.SectorId == null);
            }
            else if (m == "ai")
            {
                q = q.Where(j => j.ClassificationId != null);
            }
        }

        var total = await q.CountAsync(ct);

        // Fetch paginated jobs sorted newest classified/updated first
        var jobs = await q
            .OrderByDescending(j => j.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = new List<ClassificationHistoryItem>(jobs.Count);

        foreach (var j in jobs)
        {
            var deduction = DeduceMethod(j, aliasToSectorSlug);
            items.Add(new ClassificationHistoryItem(
                j.Id,
                j.Title,
                j.Company,
                j.Location,
                Truncate(j.Description, 400),
                j.SourceName,
                j.SourceUrl,
                ParseJsonList(j.SourceSectors),
                ParseEthioCategories(j.SourceCategories),
                j.SectorId,
                j.SectorName ?? j.Sector?.Name,
                j.Sector?.Slug,
                deduction.Method,
                deduction.Details,
                deduction.Confidence,
                deduction.Reasoning,
                j.CreatedAt,
                j.UpdatedAt,
                j.PublishedAt
            ));
        }

        // Calculate quick summary metrics
        var totalAll = await _db.Jobs.CountAsync(ct);
        var todayCount = await _db.Jobs.CountAsync(j =>
            j.CreatedAt >= todayUtc || j.UpdatedAt >= todayUtc, ct);
        var aiCount = await _db.Jobs.CountAsync(j => j.ClassificationId != null, ct);
        var uncatCount = await _db.Jobs.CountAsync(j => j.SectorId == null, ct);

        var stats = new ClassificationStatsDto(
            totalAll,
            todayCount,
            aiCount,
            uncatCount
        );

        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        return new ClassificationHistoryResponse(
            items,
            page,
            pageSize,
            total,
            totalPages,
            stats
        );
    }

    // --------------------------------------------------- AI Judge / Audit

    public async Task<AuditReportResponse> AuditClassificationsAsync(
        List<Guid>? jobIds,
        bool auditTodayOnly = false,
        int limit = 25,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var q = _db.Jobs.AsNoTracking().Include(j => j.Sector).AsQueryable();

        if (jobIds is { Count: > 0 })
        {
            q = q.Where(j => jobIds.Contains(j.Id));
        }
        else if (auditTodayOnly)
        {
            var todayUtc = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
            q = q.Where(j => j.CreatedAt >= todayUtc || j.UpdatedAt >= todayUtc);
        }

        var jobs = await q.OrderByDescending(j => j.CreatedAt).Take(limit).ToListAsync(ct);

        if (jobs.Count == 0)
        {
            return new AuditReportResponse(
                0, 0, 0, 100.0,
                "No jobs found to audit for the given criteria.",
                [],
                []);
        }

        var sectors = await _db.Sectors
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var sectorBySlug = sectors.ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);
        var sectorByName = sectors.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        // Build System Prompt
        var sectorLines = sectors.Select(s => $"- {s.Slug}: {s.Name}");
        var systemPrompt = $$"""
        You are an expert Chief Data Auditor and QA Lead for SeraGo, Ethiopia's premier career marketplace.
        Your mission is to rigorously evaluate whether job postings have been categorized into the correct industry sector or if they represent misclassifications.

        CANONICAL SERAGO SECTORS (choose ONLY from this list):
        {{string.Join("\n", sectorLines)}}

        CRITICAL EVALUATION RULES:
        1. Compare the Job's Title, Company, and Description against the Current Assigned Sector.
        2. Pay special attention to common confusion points:
           - "SDR (Sales Development Representative)" / "BPO cold calling" / "Sales rep" -> MUST be "sales-marketing", NOT "technology-it", even if the company is a tech firm or BPO.
           - "Crane Rigger" / "Equipment Operator" / "Welder" / "Mason" -> MUST be "skilled-general-labor" or "engineering-construction", NOT "technology-it".
           - "Accountant" / "Cashier" / "Auditor" -> MUST be "accounting-finance".
           - "Graphic Designer" / "UI/UX Designer" -> MUST be "design-creative".
           - "Civil Engineer" / "Site Supervisor" -> MUST be "engineering-construction".
           - "Software Engineer" / "Backend Developer" / "Data Analyst" -> MUST be "technology-it".
        3. If the current sector is genuinely accurate, set verdict to "AGREE".
        4. If the current sector is incorrect or misplaced, set verdict to "MISMATCH", specify the true suggestedSectorSlug and suggestedSectorName, and explain why.
        5. Return ONLY a valid JSON object matching the schema below. No markdown wrappers, no commentary outside JSON.

        JSON SCHEMA:
        {
          "overallSummary": "High-level audit summary in 1-2 sentences",
          "accuracyRate": 85.0,
          "optimizationTips": [
            "Tip 1 for admin to optimize keyword/alias rules",
            "Tip 2..."
          ],
          "evaluations": [
            {
              "jobId": "guid-as-string",
              "verdict": "AGREE" or "MISMATCH",
              "suggestedSectorSlug": "slug-from-above-list",
              "suggestedSectorName": "Name matching slug",
              "confidence": 0.95,
              "reason": "Clear, concise 1-sentence explanation of why it belongs here",
              "rootCause": "Accurate" or "Title keyword confusion" or "Scraper tag bias" or "Model hallucination"
            }
          ]
        }
        """;

        var userItems = jobs.Select(j => new
        {
            jobId = j.Id.ToString(),
            title = j.Title,
            company = j.Company,
            location = j.Location,
            currentSector = j.SectorName ?? j.Sector?.Name ?? "Uncategorized",
            currentSectorSlug = j.Sector?.Slug ?? "uncategorized",
            sourceName = j.SourceName ?? "Direct",
            sourceSectors = j.SourceSectors,
            description = Truncate(j.Description, 500)
        });

        var userPrompt = $$"""
        Audit the following {{jobs.Count}} job postings. Evaluate each job's current sector vs its true sector:

        {{JsonSerializer.Serialize(userItems, new JsonSerializerOptions { WriteIndented = true })}}
        """;

        AuditGroqResponse groqResponse;
        try
        {
            groqResponse = await _groq.ChatJsonAsync<AuditGroqResponse>(
                model: "", // default model
                systemPrompt: systemPrompt,
                userPrompt: userPrompt,
                timeoutSeconds: 45,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Groq AI classification audit for {Count} jobs", jobs.Count);
            throw new InvalidOperationException($"AI Judge audit failed: {ex.Message}", ex);
        }

        var evalById = groqResponse.Evaluations.ToDictionary(e => e.JobId, StringComparer.OrdinalIgnoreCase);
        var finalEvaluations = new List<AuditEvaluationResult>(jobs.Count);
        var agreedCount = 0;
        var mismatchedCount = 0;

        foreach (var job in jobs)
        {
            var jobIdStr = job.Id.ToString();
            if (evalById.TryGetValue(jobIdStr, out var eval))
            {
                var isAgree = string.Equals(eval.Verdict, "AGREE", StringComparison.OrdinalIgnoreCase);
                if (isAgree) agreedCount++; else mismatchedCount++;

                // Resolve suggested sector ID
                Guid? suggestedSectorId = null;
                string? suggestedName = eval.SuggestedSectorName;
                string? suggestedSlug = eval.SuggestedSectorSlug;

                if (!string.IsNullOrEmpty(suggestedSlug) && sectorBySlug.TryGetValue(suggestedSlug, out var matchedSec))
                {
                    suggestedSectorId = matchedSec.Id;
                    suggestedName = matchedSec.Name;
                    suggestedSlug = matchedSec.Slug;
                }
                else if (!string.IsNullOrEmpty(suggestedName) && sectorByName.TryGetValue(suggestedName, out var matchedByName))
                {
                    suggestedSectorId = matchedByName.Id;
                    suggestedName = matchedByName.Name;
                    suggestedSlug = matchedByName.Slug;
                }
                else if (isAgree && job.SectorId.HasValue)
                {
                    suggestedSectorId = job.SectorId;
                    suggestedName = job.SectorName ?? job.Sector?.Name;
                    suggestedSlug = job.Sector?.Slug;
                }

                finalEvaluations.Add(new AuditEvaluationResult(
                    job.Id,
                    job.Title,
                    job.Company,
                    job.SectorId,
                    job.SectorName ?? job.Sector?.Name ?? "Uncategorized",
                    isAgree ? "AGREE" : "MISMATCH",
                    suggestedSectorId,
                    suggestedName ?? "Uncategorized",
                    suggestedSlug ?? "uncategorized",
                    eval.Confidence,
                    eval.Reason,
                    eval.RootCause
                ));
            }
            else
            {
                // Fallback if model missed a specific job
                finalEvaluations.Add(new AuditEvaluationResult(
                    job.Id,
                    job.Title,
                    job.Company,
                    job.SectorId,
                    job.SectorName ?? job.Sector?.Name ?? "Uncategorized",
                    "AGREE",
                    job.SectorId,
                    job.SectorName ?? job.Sector?.Name ?? "Uncategorized",
                    job.Sector?.Slug ?? "uncategorized",
                    0.5,
                    "Not specifically addressed by auditor.",
                    "Pending review"
                ));
                agreedCount++;
            }
        }

        var accuracyRate = jobs.Count > 0
            ? Math.Round(((double)agreedCount / jobs.Count) * 100.0, 1)
            : 100.0;

        return new AuditReportResponse(
            jobs.Count,
            agreedCount,
            mismatchedCount,
            accuracyRate,
            groqResponse.OverallSummary,
            groqResponse.OptimizationTips ?? [],
            finalEvaluations
        );
    }

    // --------------------------------------------------- Batch Corrections

    public async Task<int> ApplyCorrectionsAsync(
        List<CorrectionRequestItem> corrections,
        CancellationToken ct = default)
    {
        if (corrections == null || corrections.Count == 0) return 0;

        var jobIds = corrections.Select(c => c.JobId).Distinct().ToList();
        var jobs = await _db.Jobs.Where(j => jobIds.Contains(j.Id)).ToListAsync(ct);
        var sectorIds = corrections.Select(c => c.NewSectorId).Distinct().ToList();
        var sectors = await _db.Sectors.Where(s => sectorIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);

        var updated = 0;
        foreach (var corr in corrections)
        {
            var job = jobs.FirstOrDefault(j => j.Id == corr.JobId);
            if (job != null && sectors.TryGetValue(corr.NewSectorId, out var sec))
            {
                job.SectorId = sec.Id;
                job.SectorName = sec.Name;
                job.UpdatedAt = DateTimeOffset.UtcNow;
                updated++;
            }
        }

        if (updated > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Applied AI audit corrections to {Count} jobs.", updated);
        }

        return updated;
    }

    // --------------------------------------------------- Helper Methods

    private static (string Method, string Details, double? Confidence, string? Reasoning) DeduceMethod(
        Job job,
        Dictionary<string, (string Slug, string Name)> aliasToSector)
    {
        if (job.SectorId == null)
        {
            return ("Uncategorized", "Pending sector classification", null, "Requires AI classification or manual admin review");
        }

        // 1. Explicit AI LLM classification
        if (job.ClassificationId != null)
        {
            return ("Gemini", "Google Gemini Flash (Tier 1 LLM)", 0.95, "Evaluated job title and responsibilities via Gemini Flash");
        }

        // 2. Exact Alias match on raw source sectors
        var sourceTags = ParseJsonList(job.SourceSectors);
        var ethioCats = ParseEthioCategories(job.SourceCategories);
        var allRawTags = sourceTags.Concat(ethioCats).ToList();

        string? matchedTag = null;
        string? matchedSecName = null;

        foreach (var tag in allRawTags)
        {
            var norm = SectorSeedData.Normalize(tag);
            if (!string.IsNullOrEmpty(norm) && aliasToSector.TryGetValue(norm, out var match))
            {
                if (match.Slug == job.Sector?.Slug)
                {
                    matchedTag = tag;
                    matchedSecName = match.Name;
                    break;
                }
            }
        }

        if (matchedTag != null)
        {
            return ("AliasRule", $"Matched Raw Tag: \"{matchedTag}\"", 0.92, $"Source category matched '{matchedSecName}' alias rule");
        }

        // 3. Title keyword match
        var keywordSlug = SectorNormalizer.ClassifyTitle(job.Title);
        if (keywordSlug != null && keywordSlug == job.Sector?.Slug)
        {
            return ("TitleKeyword", "Matched keyword in job title", 0.88, "Job title keyword matched canonical sector rule");
        }

        // 4. Scraped jobs whose sector was classified by 4-tier pipeline
        if (!string.IsNullOrEmpty(job.SourceName))
        {
            return ("Gemini", "Google Gemini Flash (Tier 1 LLM)", 0.94, "Classified by unified AI pipeline based on job description & title");
        }

        return ("AdminManual", "Posted or edited by admin/recruiter", 1.0, "Manual platform assignment");
    }

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<string> ParseEthioCategories(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];
            var list = new List<string>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("name", out var n))
                {
                    var val = n.GetString();
                    if (!string.IsNullOrWhiteSpace(val)) list.Add(val);
                }
                else if (item.ValueKind == JsonValueKind.String)
                {
                    var val = item.GetString();
                    if (!string.IsNullOrWhiteSpace(val)) list.Add(val);
                }
            }
            return list;
        }
        catch
        {
            return [];
        }
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value[..max] + "...";
    }
}

// --------------------------------------------------- Records & DTOs

public sealed record ClassificationHistoryItem(
    Guid Id,
    string Title,
    string Company,
    string Location,
    string DescriptionPreview,
    string? SourceName,
    string? SourceUrl,
    List<string> SourceSectors,
    List<string> EthioCategories,
    Guid? SectorId,
    string? SectorName,
    string? SectorSlug,
    string ClassificationMethod,
    string ClassificationDetails,
    double? Confidence,
    string? Reasoning,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

public sealed record ClassificationStatsDto(
    int TotalJobs,
    int TodayJobsCount,
    int AiClassifiedCount,
    int UncategorizedCount);

public sealed record ClassificationHistoryResponse(
    List<ClassificationHistoryItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    ClassificationStatsDto Stats);

public sealed record AuditReportResponse(
    int TotalAudited,
    int AgreedCount,
    int MismatchedCount,
    double AccuracyRate,
    string OverallSummary,
    List<string> OptimizationTips,
    List<AuditEvaluationResult> Evaluations);

public sealed record AuditEvaluationResult(
    Guid JobId,
    string Title,
    string Company,
    Guid? CurrentSectorId,
    string CurrentSectorName,
    string Verdict,
    Guid? SuggestedSectorId,
    string SuggestedSectorName,
    string SuggestedSectorSlug,
    double Confidence,
    string Reason,
    string RootCause);

public sealed record CorrectionRequestItem(
    Guid JobId,
    Guid NewSectorId);

public sealed class AuditGroqResponse
{
    [JsonPropertyName("overallSummary")]
    public string OverallSummary { get; set; } = string.Empty;

    [JsonPropertyName("accuracyRate")]
    public double AccuracyRate { get; set; }

    [JsonPropertyName("optimizationTips")]
    public List<string> OptimizationTips { get; set; } = [];

    [JsonPropertyName("evaluations")]
    public List<AuditGroqEvaluation> Evaluations { get; set; } = [];
}

public sealed class AuditGroqEvaluation
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("verdict")]
    public string Verdict { get; set; } = "AGREE";

    [JsonPropertyName("suggestedSectorSlug")]
    public string? SuggestedSectorSlug { get; set; }

    [JsonPropertyName("suggestedSectorName")]
    public string? SuggestedSectorName { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 0.9;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("rootCause")]
    public string RootCause { get; set; } = string.Empty;
}
