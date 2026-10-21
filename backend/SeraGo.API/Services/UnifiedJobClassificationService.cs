using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.API.Services;

/// <summary>
/// 4-Tier unified job classification pipeline:
///   Tier 1: Google Gemini Flash (Multilingual / Amharic / High Speed)
///   Tier 2: Groq Cloud LLM (Ultra-fast fallback)
///   Tier 3: Local Ollama (Offline / On-Premise fallback)
///   Tier 4: Semantic Cosine Similarity against historical classified jobs
/// </summary>
public sealed class UnifiedJobClassificationService
{
    private readonly ApplicationDbContext _db;
    private readonly GeminiClient _gemini;
    private readonly GroqClient _groq;
    private readonly OllamaClient _ollama;
    private readonly ILogger<UnifiedJobClassificationService> _logger;

    public UnifiedJobClassificationService(
        ApplicationDbContext db,
        GeminiClient gemini,
        GroqClient groq,
        OllamaClient ollama,
        ILogger<UnifiedJobClassificationService> logger)
    {
        _db = db;
        _gemini = gemini;
        _groq = groq;
        _ollama = ollama;
        _logger = logger;
    }

    /// <summary>
    /// Classifies a batch of jobs through the 4-tier pipeline and saves the updated sectors to the database.
    /// </summary>
    public async Task<List<UnifiedJobClassificationResult>> ClassifyAndSaveAsync(
        IReadOnlyList<Job> jobs,
        CancellationToken ct = default)
    {
        if (jobs == null || jobs.Count == 0) return [];

        var results = await ClassifyJobsAsync(jobs, ct);

        // Preload active sectors
        var sectors = await _db.Sectors
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var sectorBySlug = sectors.ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);
        var sectorById = sectors.ToDictionary(s => s.Id);

        var updatedCount = 0;
        foreach (var r in results)
        {
            var job = jobs.FirstOrDefault(j => j.Id == r.JobId);
            if (job == null) continue;

            Sector? assignedSector = null;
            if (r.SectorId.HasValue && sectorById.TryGetValue(r.SectorId.Value, out var sIdMatch))
            {
                assignedSector = sIdMatch;
            }
            else if (!string.IsNullOrEmpty(r.SectorSlug) && sectorBySlug.TryGetValue(r.SectorSlug, out var sSlugMatch))
            {
                assignedSector = sSlugMatch;
            }

            if (assignedSector != null)
            {
                job.SectorId = assignedSector.Id;
                job.SectorName = assignedSector.Name;
                job.UpdatedAt = DateTimeOffset.UtcNow;
                updatedCount++;
            }
            else
            {
                job.SectorId = null;
                job.SectorName = null;
            }
        }

        if (updatedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Unified classifier updated {Count}/{Total} jobs.", updatedCount, jobs.Count);
        }

        return results;
    }

    /// <summary>
    /// Evaluates jobs through Tier 1 (Gemini) -> Tier 2 (Groq) -> Tier 3 (Ollama) -> Tier 4 (Cosine Similarity).
    /// </summary>
    public async Task<List<UnifiedJobClassificationResult>> ClassifyJobsAsync(
        IReadOnlyList<Job> jobs,
        CancellationToken ct = default)
    {
        if (jobs == null || jobs.Count == 0) return [];

        var sectors = await _db.Sectors
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var sectorBySlug = sectors.ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);
        var sectorLines = sectors.Select(s => $"- {s.Slug}: {s.Name}");

        var systemPrompt = $$"""
        You are an expert Chief Job Classifier for SeraGo, Ethiopia's career marketplace.
        Classify each job into EXACTLY ONE canonical sector from the active list below.

        CANONICAL SECTORS:
        {{string.Join("\n", sectorLines)}}

        CRITICAL CLASSIFICATION RULES:
        1. Base your decision primarily on the Job Title and core responsibilities in the Description.
        2. SALES & MARKETING:
           - "SDR" / "Sales Development Representative" / "BPO cold calling" / "Telemarketer" -> MUST be "sales-marketing", NOT "technology-it".
           - "Real Estate Agent" / "Property Sales" / "Sales Consultant" / "Salesperson" / "Commercial Sales" -> MUST be "sales-marketing", NOT "accounting-finance" or "business-administration".
        3. BANKING & INSURANCE:
           - Bank branch operations ("Branch Operations Manager", "Branch Business Relationship Manager", "Customer Relationship Manager", "IFB Operations", "Financing and Investment Officer") at a commercial bank (e.g., Gadaa Bank, Ahadu Bank, Dashen Bank, Siinqee Bank) -> MUST be "banking-insurance", NOT generic "business-administration" or "customer-service-support".
        4. PROCUREMENT & SUPPLY CHAIN vs LOGISTICS:
           - "Storekeeper" / "Store Keeper" / "Warehouse Materials Keeper" / "Purchasing Officer" / "Procurement and Logistics Officer" -> MUST be "procurement-supply-chain" or "logistics-transportation", NOT "sales-marketing" or "business-administration".
        5. TECHNOLOGY & IT:
           - "IT Officer", "Hardware Technician", "Computer Technician", "Network Engineer", "Software Developer", "DevOps", "Database Admin", "IS Trainee" -> MUST be "technology-it", NOT "skilled-general-labor".
        6. HEALTHCARE:
           - Doctor, Nurse, Midwife, Pharmacist, Lab Technician, "Hygiene & Sanitation Officer" -> MUST be "healthcare", NOT "security-protection".
        7. EDUCATION & TRAINING:
           - Teacher (English Teacher, Science Teacher, Kindergarten Teacher, Amharic Teacher), Lecturer, Trainer -> MUST be "education-training", NOT "design-creative".
        8. HUMAN RESOURCES:
           - HR Officer, HR Specialist, HR Generalist, Talent Acquisition, Recruiter -> MUST be "human-resources", NOT "business-administration".
        9. ENGINEERING & CONSTRUCTION vs SKILLED LABOR:
           - Civil Engineer, Structural Engineer, Electrical Works Coordinator, Surveyor, Surveying Team Leader, Building Project Coordinator -> MUST be "engineering-construction".
           - Crane Rigger, Equipment Operator, Welder, Mason, Auto Mechanic, Gomista/Tireman -> MUST be "skilled-general-labor" or "engineering-construction".
        10. SECURITY & PROTECTION:
           - Security Guard, Security Supervisor, Security Officer -> MUST be "security-protection".
        11. ACCOUNTING & FINANCE:
           - Accountant, Senior Accountant, Cashier, Auditor, Financial Analyst, Budget Officer -> MUST be "accounting-finance".
        12. DESIGN & CREATIVE vs MEDIA:
           - Graphic Designer, UI/UX, Motion Designer -> "design-creative".
           - Video Editor, Videographer, Cameraman, Journalist, TV/Podcast Host -> "media-communication".
        13. If none fit, set sectorSlug to null.
        14. Return ONLY a valid JSON object matching this schema:
        {
          "results": [
            {
              "jobId": "guid-as-string",
              "sectorSlug": "slug-from-above-or-null",
              "confidence": 0.95,
              "reasoning": "Short 1-sentence reason"
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
            sourceName = j.SourceName ?? "Direct",
            sourceSectors = j.SourceSectors,
            description = Truncate(j.Description, 500)
        });

        var userPrompt = $$"""
        Classify the following {{jobs.Count}} job postings into canonical sectors:

        {{JsonSerializer.Serialize(userItems, new JsonSerializerOptions { WriteIndented = true })}}
        """;

        // ── TIER 1: Google Gemini Flash ───────────────────────────────────────
        if (_gemini.IsConfigured)
        {
            try
            {
                _logger.LogInformation("Attempting Tier 1 (Gemini Flash) classification for {Count} jobs.", jobs.Count);
                var geminiEnvelope = await _gemini.GenerateJsonAsync<ClassificationEnvelope>(
                    systemPrompt, userPrompt, ct);

                if (geminiEnvelope?.Results is { Count: > 0 })
                {
                    return MapResults(jobs, geminiEnvelope.Results, sectorBySlug, "Gemini");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tier 1 (Gemini) classification failed or was rate-limited. Falling back to Tier 2 (Groq).");
            }
        }

        // ── TIER 2: Groq Cloud LLM ───────────────────────────────────────────
        if (_groq.IsConfigured)
        {
            try
            {
                _logger.LogInformation("Attempting Tier 2 (Groq) classification for {Count} jobs.", jobs.Count);
                var groqEnvelope = await _groq.ChatJsonAsync<ClassificationEnvelope>(
                    model: "",
                    systemPrompt: systemPrompt,
                    userPrompt: userPrompt,
                    timeoutSeconds: 30,
                    ct: ct);

                if (groqEnvelope?.Results is { Count: > 0 })
                {
                    return MapResults(jobs, groqEnvelope.Results, sectorBySlug, "Groq");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tier 2 (Groq) classification failed. Falling back to Tier 3 (Ollama).");
            }
        }

        // ── TIER 3: Local Ollama (Offline / On-Prem) ──────────────────────────
        try
        {
            _logger.LogInformation("Attempting Tier 3 (Local Ollama) classification for {Count} jobs.", jobs.Count);
            var ollamaEnvelope = await _ollama.ChatJsonAsync<ClassificationEnvelope>(
                systemPrompt, userPrompt, ct);

            if (ollamaEnvelope?.Results is { Count: > 0 })
            {
                return MapResults(jobs, ollamaEnvelope.Results, sectorBySlug, "Ollama");
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Tier 3 (Ollama) unavailable or failed: {Message}. Falling back to Tier 4 (Cosine Similarity).", ex.Message);
        }

        // ── TIER 4: Semantic Cosine Similarity against historical classified jobs ─
        _logger.LogInformation("Executing Tier 4 (Semantic Cosine Similarity) fallback for {Count} jobs.", jobs.Count);
        return await FallbackCosineSimilarityAsync(jobs, sectors, ct);
    }

    private static List<UnifiedJobClassificationResult> MapResults(
        IReadOnlyList<Job> jobs,
        List<ClassificationItemResult> items,
        Dictionary<string, Sector> sectorBySlug,
        string method)
    {
        var map = items.ToDictionary(i => i.JobId, StringComparer.OrdinalIgnoreCase);
        var finalResults = new List<UnifiedJobClassificationResult>(jobs.Count);

        foreach (var j in jobs)
        {
            var idStr = j.Id.ToString();
            if (map.TryGetValue(idStr, out var res) && !string.IsNullOrEmpty(res.SectorSlug))
            {
                if (sectorBySlug.TryGetValue(res.SectorSlug, out var sec))
                {
                    finalResults.Add(new UnifiedJobClassificationResult(
                        j.Id,
                        sec.Id,
                        sec.Name,
                        sec.Slug,
                        res.Confidence,
                        res.Reasoning ?? $"{method} classification",
                        method,
                        null));
                    continue;
                }
            }

            finalResults.Add(new UnifiedJobClassificationResult(
                j.Id,
                null,
                null,
                null,
                0.0,
                "No sector matched or classified uncategorized",
                method,
                null));
        }

        return finalResults;
    }

    /// <summary>
    /// Tier 4 Fallback: Finds nearest historical jobs by semantic term overlap (cosine-normalized bag-of-words / TF-IDF)
    /// across already-classified jobs in the database.
    /// </summary>
    private async Task<List<UnifiedJobClassificationResult>> FallbackCosineSimilarityAsync(
        IReadOnlyList<Job> jobs,
        List<Sector> activeSectors,
        CancellationToken ct)
    {
        var results = new List<UnifiedJobClassificationResult>(jobs.Count);

        // Load reference sample of recently classified jobs
        var referenceJobs = await _db.Jobs
            .AsNoTracking()
            .Where(j => j.SectorId != null)
            .OrderByDescending(j => j.CreatedAt)
            .Take(400)
            .Select(j => new { j.Id, j.Title, j.Description, j.SectorId, j.SectorName })
            .ToListAsync(ct);

        var sectorById = activeSectors.ToDictionary(s => s.Id);

        foreach (var job in jobs)
        {
            var jobTokens = Tokenize($"{job.Title} {Truncate(job.Description, 200)}");
            double bestSimilarity = 0.0;
            Guid? bestSectorId = null;
            string? bestSectorName = null;

            foreach (var r in referenceJobs)
            {
                if (!r.SectorId.HasValue) continue;
                var refTokens = Tokenize($"{r.Title} {Truncate(r.Description, 200)}");
                var sim = ComputeCosineSimilarity(jobTokens, refTokens);

                if (sim > bestSimilarity)
                {
                    bestSimilarity = sim;
                    bestSectorId = r.SectorId.Value;
                    bestSectorName = r.SectorName;
                }
            }

            // Acceptance threshold
            if (bestSimilarity >= 0.70 && bestSectorId.HasValue && sectorById.TryGetValue(bestSectorId.Value, out var matchedSec))
            {
                results.Add(new UnifiedJobClassificationResult(
                    job.Id,
                    matchedSec.Id,
                    matchedSec.Name,
                    matchedSec.Slug,
                    bestSimilarity,
                    $"Nearest neighbor match ({Math.Round(bestSimilarity * 100)}% similarity)",
                    "CosineSimilarity",
                    null));
            }
            else
            {
                results.Add(new UnifiedJobClassificationResult(
                    job.Id,
                    null,
                    null,
                    null,
                    bestSimilarity,
                    "Low semantic similarity to past jobs; marked for admin review",
                    "Uncategorized",
                    null));
            }
        }

        return results;
    }

    private static Dictionary<string, double> Tokenize(string text)
    {
        var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text)) return dict;

        var words = text.ToLowerInvariant()
            .Split([' ', ',', '.', ':', ';', '(', ')', '[', ']', '-', '/', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var w in words)
        {
            if (w.Length <= 2) continue; // skip noise words
            dict[w] = dict.GetValueOrDefault(w, 0.0) + 1.0;
        }

        // Normalize vector length (L2 norm)
        double sumSquares = 0.0;
        foreach (var val in dict.Values) sumSquares += val * val;
        var norm = Math.Sqrt(sumSquares);

        if (norm > 0)
        {
            foreach (var k in dict.Keys.ToList()) dict[k] /= norm;
        }

        return dict;
    }

    private static double ComputeCosineSimilarity(Dictionary<string, double> vecA, Dictionary<string, double> vecB)
    {
        if (vecA.Count == 0 || vecB.Count == 0) return 0.0;
        double dotProduct = 0.0;
        foreach (var (term, weightA) in vecA)
        {
            if (vecB.TryGetValue(term, out var weightB))
            {
                dotProduct += weightA * weightB;
            }
        }
        return dotProduct;
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value[..max] + "...";
    }
}

// ── DTOs ───────────────────────────────────────────────────────────────

public sealed class ClassificationEnvelope
{
    [JsonPropertyName("results")]
    public List<ClassificationItemResult> Results { get; set; } = [];
}

public sealed class ClassificationItemResult
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("sectorSlug")]
    public string? SectorSlug { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 0.9;

    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }
}

public sealed record UnifiedJobClassificationResult(
    Guid JobId,
    Guid? SectorId,
    string? SectorName,
    string? SectorSlug,
    double Confidence,
    string Reasoning,
    string Method,
    string? Error);
