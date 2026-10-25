using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.API.Services;

/// <summary>
/// 5-Tier unified job classification pipeline:
///   Tier 1: OpenRouter (NVIDIA Nemotron 3 Ultra 550B / Google DeepMind Gemma 4 with Dual-Key Failover)
///   Tier 2: Groq Cloud LLM (Meta Llama 3.3 70B - 1,000 free req/day)
///   Tier 3: Google Gemini (Gemini 2.0 Flash / Flash Thinking - 1,500 free req/day)
///   Tier 4: Local Ollama (Offline / On-Premise fallback)
///   Tier 5: Semantic Cosine Similarity against historical classified jobs
/// </summary>
public sealed class UnifiedJobClassificationService
{
    private readonly ApplicationDbContext _db;
    private readonly OpenRouterClient _openRouter;
    private readonly GroqClient _groq;
    private readonly GeminiClient _gemini;
    private readonly OllamaClient _ollama;
    private readonly ILogger<UnifiedJobClassificationService> _logger;

    public UnifiedJobClassificationService(
        ApplicationDbContext db,
        OpenRouterClient openRouter,
        GroqClient groq,
        GeminiClient gemini,
        OllamaClient ollama,
        ILogger<UnifiedJobClassificationService> logger)
    {
        _db = db;
        _openRouter = openRouter;
        _groq = groq;
        _gemini = gemini;
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
        var now = DateTimeOffset.UtcNow;

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

            string modelUsed;
            string modelTier;

            switch (r.Method.ToLowerInvariant())
            {
                case "openrouter nemotron":
                case "openrouter":
                case "nemotron":
                    modelUsed = "OpenRouter Nemotron";
                    modelTier = "Tier 1 (NVIDIA Nemotron 3 Ultra 550B via OpenRouter)";
                    break;
                case "groq":
                case "groq llama":
                    modelUsed = "Groq Llama 3.3 70B";
                    modelTier = "Tier 2 (Meta Llama 3.3 70B via Groq Cloud)";
                    break;
                case "gemini":
                case "google gemini":
                    modelUsed = "Google Gemini 2.0 Flash";
                    modelTier = "Tier 3 (Google Gemini 2.0 Flash)";
                    break;
                case "ollama":
                case "local ollama":
                    modelUsed = "Local Ollama";
                    modelTier = "Tier 4 (Local Ollama Offline LLM)";
                    break;
                case "cosine":
                case "cosinesimilarity":
                    modelUsed = "Cosine Similarity";
                    modelTier = "Tier 5 (Vector Semantic Similarity Fallback)";
                    break;
                default:
                    modelUsed = r.Method;
                    modelTier = $"AI Model ({r.Method})";
                    break;
            }

            if (assignedSector != null)
            {
                job.SectorId = assignedSector.Id;
                job.SectorName = assignedSector.Name;
                job.UpdatedAt = now;
                updatedCount++;

                var record = new JobClassificationRecord
                {
                    Id = Guid.NewGuid(),
                    JobId = job.Id,
                    ModelUsed = modelUsed,
                    ModelTier = modelTier,
                    SectorSlug = assignedSector.Slug,
                    SectorName = assignedSector.Name,
                    Confidence = r.Confidence,
                    Reasoning = r.Reasoning,
                    ClassifiedAt = now
                };

                _db.JobClassificationRecords.Add(record);
                job.ClassificationId = record.Id;
            }
            else
            {
                job.SectorId = null;
                job.SectorName = null;

                var record = new JobClassificationRecord
                {
                    Id = Guid.NewGuid(),
                    JobId = job.Id,
                    ModelUsed = modelUsed,
                    ModelTier = modelTier,
                    SectorSlug = null,
                    SectorName = null,
                    Confidence = r.Confidence,
                    Reasoning = !string.IsNullOrWhiteSpace(r.Reasoning) ? r.Reasoning : "No confident sector match found across taxonomy",
                    ClassifiedAt = now
                };

                _db.JobClassificationRecords.Add(record);
                job.ClassificationId = record.Id;
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
        1. Base your decision primarily on the Job Title, required qualifications, and core responsibilities in the Description snippet, NOT blindly trusting ambiguous raw scraper tags (e.g. scraper tags like "Economics" or "Business" are often misapplied to refugee, MEAL, or healthcare roles).

        2. SALES & MARKETING:
           - "SDR" / "Sales Development Representative" / "BPO cold calling" / "Telemarketer" / "Account Executive" -> MUST be "sales-marketing", NOT "technology-it".
           - "Real Estate Agent" / "Property Sales" / "Sales Consultant" / "Salesperson" / "Commercial Sales" -> MUST be "sales-marketing", NOT "accounting-finance" or "business-administration".
           - "Pre-Sales Team Lead" / "Business Development Executive" / "Market Research Officer" -> MUST be "sales-marketing".
           - Amharic: "የሽያጭ ሰራተኛ", "ገበያ ጥናት" -> MUST be "sales-marketing".

        3. BANKING & INSURANCE vs BUSINESS ADMINISTRATION:
           - Bank branch operations & branch managers ("Branch Manager", "Branch Business Manager", "Branch Operations Manager", "Customer Relationship Manager", "IFB Operations", "Financing and Investment Officer") at commercial banks (e.g., Dashen Bank, CBE, Awash, Ahadu Bank, Siinqee Bank, Gadaa Bank, Nib, Abyssinia) -> MUST be "banking-insurance", NOT generic "business-administration".
           - Microfinance loan and credit analysis ("Credit Officer", "Senior Credit Officer", "Loan Officer", "Credit Portfolio Analyst") -> MUST be "banking-insurance", NEVER "technology-it".
           - Insurance professionals ("Insurance Officer", "Underwriter", "Claims Officer") -> MUST be "banking-insurance".

        4. TECHNOLOGY & IT vs INDUSTRY EMPLOYERS (Role vs Industry):
           - "Software Engineer", "Backend Developer", "Full Stack", "Cloud Engineer", "DevOps", "Database Admin", "Network Administrator", "IT Support Officer", "Hardware Technician", "IS Trainee" -> MUST be "technology-it".
           - TELECOM & SOFTWARE PLATFORMS: "OCS Billing Engineer" / "Charging System Engineer" / "CDR Analyst", "RBT Operation Engineer" (Ringback Tone), "VAS Specialist", "Core Network Product Engineer", "IP Service Engineer", "Microwave Engineer", "Cloud Core Engineer", "NGBSS Test Engineer", "Mobile Money Maintenance Engineer", "Network Planning & Optimization" -> MUST be "technology-it", NEVER "engineering-construction"!
           - CRITICAL: "Core Banking System Admin" / "Banking IT Officer" / "Hospital IT Administrator" -> MUST be "technology-it", even if employed by a bank or hospital! Technical IT roles always take precedence over the employer's industry.

        5. ENGINEERING & CONSTRUCTION vs ACCOUNTING & AUDIT (Construction lifecycle disambiguation):
           - When a job is at an Engineering/Construction company or requires a Civil/Mechanical/Electrical Engineering degree:
             * "Monitoring and Reporting Division Head" -> "engineering-construction" (monitoring civil works and site progress, NOT financial reporting).
             * "Project Closing Division Head" -> "engineering-construction" (contractual close-out and commissioning of physical infrastructure, NOT financial year-end closing).
             * "Project Performance Audit Division Head" -> "engineering-construction" (civil engineering worksite performance auditing, NOT financial/tax auditing).
             * "Bid Management Division Head" / "Tender & Estimation Specialist" / "BOQ Specialist" / "Quantity Surveyor" -> "engineering-construction".
             * "Equipment Rental Division Head" / "Rental Officer" / "Concrete Pump Operator" / "Line Pump Operator" -> "engineering-construction".
           - Civil Engineer, Structural Engineer, Architect, Landscape Designer, Site Supervisor -> "engineering-construction".

        6. SOCIAL SCIENCE & COMMUNITY vs ACCOUNTING (The "Economics" & Humanitarian Trap):
           - In NGOs and humanitarian programs, "Economic Strengthening Specialist", "Livelihoods Specialist/Officer", "VSLA Facilitator", "Community Engagement Facilitator", "Reintegration Associate" -> MUST be "social-science-community", NOT "accounting-finance" (these are household food security, poverty alleviation, and community empowerment roles).
           - "GCR Advisor" (Global Compact on Refugees), Refugee Protection, Migration Advisor, Human Rights Officer -> "social-science-community" (or "legal" for legal counsel).
           - "MEAL" / "M&E" (Monitoring, Evaluation, Accountability, and Learning) Officers and Interns, "Data Collector" in NGOs -> MUST be "social-science-community", NOT "accounting-finance".

        7. HEALTHCARE & MEDICAL:
           - Doctor, Medical Internist, General Practitioner, Anesthetist, Nurse, Home Care Nurse, Midwife, Pharmacist, Druggist, Lab Technician -> MUST be "healthcare".
           - Medical health programs: "NCD Project Coordinator" (Non-Communicable Diseases), "RMNCH Officer" (Reproductive, Maternal, Newborn & Child Health), "HIV Prevention Officer", "HIV Service Officer" -> MUST be "healthcare", NEVER "engineering-construction" or "security-protection".

        8. WORKPLACE SAFETY & HSE vs SECURITY:
           - "Workplace Safety Officer", "Senior Environment and Workplace Safety Officer", "HSE Coordinator", "Environmental Health & Safety" on construction sites or factories -> MUST be "engineering-construction" (if on civil worksites) or "healthcare" (occupational hygiene), NEVER "security-protection".
           - "security-protection" is STRICTLY reserved for physical security guards ("ጥበቃ"), watchmen, bodyguards, and security escorts.

        9. PROCUREMENT & SUPPLY CHAIN vs LOGISTICS:
           - "Purchaser", "Purchasing Officer", "Senior Procurement", "Procurement Specialist", "Buyer", "Sourcing Officer", "Storekeeper", "Store Keeper", "Warehouse Materials Keeper" -> MUST be "procurement-supply-chain", NOT "logistics-transportation" or "sales-marketing".
           - Drivers (truck driver, mixer driver, light vehicle driver, motorbike delivery driver, courier), Fleet Officer, Dispatcher, Freight Forwarder, Customs Clearance -> "logistics-transportation".

        10. SKILLED & GENERAL LABOR:
           - "Battery Swapper" (electric vehicle battery swapping attendant), Tailor / Garment Maker ("የልብስ ስፌት"), Woodworker / Carpenter ("የእንጨት ሥራ"), Furniture Machinist, Ceramic Professional / Tiler ("የሴራሚክ ባለሙያ"), Welder, Mason, Auto Mechanic, Janitor/Cleaner -> MUST be "skilled-general-labor".

        11. BUSINESS & ADMINISTRATION vs EDUCATION / CLERICAL:
           - Secretaries, Executive Assistants, Office Administrators, Archive Officers -> "business-administration".
           - CRITICAL: "TVET Secretary" or "School Secretary" is secretarial/clerical and belongs in "business-administration", NOT "education-training" (which is reserved for teachers, lecturers, and instructors).

        12. MEDIA & COMMUNICATION vs DESIGN & CREATIVE:
           - Video Editor, Videographer, Social Media Manager, Content Writer, Journalist, Communications Manager, Public Relations -> "media-communication".
           - Graphic Designer, UI/UX Designer, Motion Graphics Artist, Creative Director -> "design-creative".

        13. ACCOUNTING & FINANCE (Role vs Employer Industry Trap):
           - "Accountant", "Senior Accountant", "Junior Accountant", "Cost Accountant", "Project Accountant", "Finance Officer", "Finance Manager", "Auditor", "Internal Auditor", "Cashier", "Disbursement Officer", "Tax Officer" -> MUST ALWAYS be "accounting-finance".
           - AMHARIC ACCOUNTING TITLES: "አካውንታንት", "ሲኒየር አካውንታንት", "ሲኒዬር አካውንታንት", "ጁኒየር አካውንታንት", "የሂሳብ ባለሙያ", "ሂሳብ ሹም", "ዋና ሂሳብ ሹም", "ገንዘብ ያዥ", "ካሸር", "ፋይናንስ ኦፊሰር" -> MUST ALWAYS be "accounting-finance".
           - CRITICAL TRAP: When an accountant or cashier works at a School, College, University, Training Institute, Factory, Hotel, Hospital, or NGO, their role is financial accounting, NOT teaching, healthcare, or hospitality! They MUST be classified as "accounting-finance".

        14. If none fit, set sectorSlug to null.
        15. Return ONLY a valid JSON object matching this schema:
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

        if (jobs.Count > 20)
        {
            var chunks = jobs.Chunk(20).ToList();
            _logger.LogInformation(
                "Batch of {Total} jobs split into {Count} micro-batches of <= 20 jobs for maximum classification accuracy.",
                jobs.Count, chunks.Count);

            var aggregatedResults = new List<UnifiedJobClassificationResult>(jobs.Count);
            for (var i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var chunkResults = await EvaluateChunkAsync(chunk, sectors, sectorBySlug, systemPrompt, ct);
                aggregatedResults.AddRange(chunkResults);

                if (i + 1 < chunks.Count)
                {
                    await Task.Delay(1000, ct);
                }
            }
            return aggregatedResults;
        }

        return await EvaluateChunkAsync(jobs, sectors, sectorBySlug, systemPrompt, ct);
    }

    private async Task<List<UnifiedJobClassificationResult>> EvaluateChunkAsync(
        IReadOnlyList<Job> chunk,
        List<Sector> sectors,
        Dictionary<string, Sector> sectorBySlug,
        string systemPrompt,
        CancellationToken ct)
    {
        var userItems = chunk.Select(j => new
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
        Classify the following {{chunk.Count}} job postings into canonical sectors:

        {{JsonSerializer.Serialize(userItems, new JsonSerializerOptions { WriteIndented = true })}}
        """;

        // ── TIER 1: OpenRouter (NVIDIA Nemotron 3 Ultra 550B / Google Gemma 4 with Dual-Key Failover) ──
        if (_openRouter.IsConfigured)
        {
            try
            {
                _logger.LogInformation("Attempting Tier 1 (OpenRouter Nemotron 550B) classification for {Count} jobs.", chunk.Count);
                var openRouterEnvelope = await _openRouter.ChatJsonAsync<ClassificationEnvelope>(
                    systemPrompt, userPrompt, timeoutSeconds: 35, ct: ct);

                if (openRouterEnvelope?.Results is { Count: > 0 })
                {
                    return MapResults(chunk, openRouterEnvelope.Results, sectorBySlug, "OpenRouter Nemotron");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tier 1 (OpenRouter) classification failed or was rate-limited. Falling back to Tier 2 (Groq).");
            }
        }

        // ── TIER 2: Groq Cloud LLM (Meta Llama 3.3 70B - 1,000 free req/day) ──
        if (_groq.IsConfigured)
        {
            try
            {
                _logger.LogInformation("Attempting Tier 2 (Groq Llama 3.3 70B) classification for {Count} jobs.", chunk.Count);
                var groqEnvelope = await _groq.ChatJsonAsync<ClassificationEnvelope>(
                    model: "",
                    systemPrompt: systemPrompt,
                    userPrompt: userPrompt,
                    timeoutSeconds: 30,
                    ct: ct);

                if (groqEnvelope?.Results is { Count: > 0 })
                {
                    return MapResults(chunk, groqEnvelope.Results, sectorBySlug, "Groq");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tier 2 (Groq) classification failed. Falling back to Tier 3 (Google Gemini).");
            }
        }

        // ── TIER 3: Google Gemini (Gemini 2.0 Flash / Flash Thinking - 1,500 free req/day) ──
        if (_gemini.IsConfigured)
        {
            try
            {
                _logger.LogInformation("Attempting Tier 3 (Gemini 2.0 Flash) classification for {Count} jobs.", chunk.Count);
                var geminiEnvelope = await _gemini.GenerateJsonAsync<ClassificationEnvelope>(
                    systemPrompt, userPrompt, ct);

                if (geminiEnvelope?.Results is { Count: > 0 })
                {
                    return MapResults(chunk, geminiEnvelope.Results, sectorBySlug, "Gemini");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tier 3 (Gemini) classification failed or was rate-limited. Falling back to Tier 4 (Ollama).");
            }
        }

        // ── TIER 4: Local Ollama (Offline / On-Prem) ──────────────────────────
        try
        {
            _logger.LogInformation("Attempting Tier 4 (Local Ollama) classification for {Count} jobs.", chunk.Count);
            var ollamaEnvelope = await _ollama.ChatJsonAsync<ClassificationEnvelope>(
                systemPrompt, userPrompt, ct);

            if (ollamaEnvelope?.Results is { Count: > 0 })
            {
                return MapResults(chunk, ollamaEnvelope.Results, sectorBySlug, "Ollama");
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Tier 4 (Ollama) unavailable or failed: {Message}. Falling back to Tier 5 (Cosine Similarity).", ex.Message);
        }

        // ── TIER 5: Semantic Cosine Similarity against historical classified jobs ─
        _logger.LogInformation("Executing Tier 5 (Semantic Cosine Similarity) fallback for {Count} jobs.", chunk.Count);
        return await FallbackCosineSimilarityAsync(chunk, sectors, ct);
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
                    var reasoningText = !string.IsNullOrWhiteSpace(res.Reasoning)
                        ? $"[{method}] {res.Reasoning}"
                        : $"[{method}] Sector assigned based on title and core responsibilities";

                    finalResults.Add(new UnifiedJobClassificationResult(
                        j.Id,
                        sec.Id,
                        sec.Name,
                        sec.Slug,
                        res.Confidence,
                        reasoningText,
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
                $"[{method}] No sector matched or classified uncategorized",
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
