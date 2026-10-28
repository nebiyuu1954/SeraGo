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
    private readonly IServiceScopeFactory _scopeFactory;

    public UnifiedJobClassificationService(
        ApplicationDbContext db,
        OpenRouterClient openRouter,
        GroqClient groq,
        GeminiClient gemini,
        OllamaClient ollama,
        ILogger<UnifiedJobClassificationService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _openRouter = openRouter;
        _groq = groq;
        _gemini = gemini;
        _ollama = ollama;
        _logger = logger;
        _scopeFactory = scopeFactory;
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
                case "groq llama 8b instant":
                case "groq llama 8b":
                case "groq 8b":
                    modelUsed = "Groq Llama 3.1 8B Instant";
                    modelTier = "Tier 2-Fast (Meta Llama 3.1 8B Instant via Groq Cloud)";
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
           - Amharic: "የሽያጭ ሰራተኛ", "የሱቅ ሱፐርቫይዘር" (Shop/Retail Store Supervisor) -> MUST be "sales-marketing", NOT "hospitality-tourism".
           - "የገበያ ጥናትና ልማት መምሪያ ሥራ አስኪያጅ" / "የገበያ ጥናት" -> MUST be "sales-marketing", regardless of whether the employer is a church, school, or NGO.

        3. BANKING & INSURANCE vs BUSINESS ADMINISTRATION:
           - Bank branch operations & branch managers ("Branch Manager", "Branch Business Manager", "Branch Operations Manager", "Customer Relationship Manager", "IFB Operations", "Financing and Investment Officer") at commercial banks (e.g., Dashen Bank, CBE, Awash, Ahadu Bank, Siinqee Bank, Gadaa Bank, Nib, Abyssinia) -> MUST be "banking-insurance", NOT generic "business-administration".
           - Microfinance loan and credit analysis ("Credit Officer", "Senior Credit Officer", "Loan Officer", "Relationship Manager-Credit", "Credit Portfolio Analyst") -> MUST be "banking-insurance", NEVER "accounting-finance" or "technology-it".
           - Insurance professionals ("Insurance Officer", "Underwriter", "Claims Officer") -> MUST be "banking-insurance".

        4. TECHNOLOGY & IT vs INDUSTRY EMPLOYERS (Role vs Industry):
           - "Software Engineer", "Backend Developer", "Full Stack", "Cloud Engineer", "DevOps", "Database Admin", "Network Administrator", "IT Support Officer", "Hardware Technician", "IS Trainee", "Data Analyst" -> MUST be "technology-it".
           - TELECOM & SOFTWARE PLATFORMS: "OCS Billing Engineer" / "Charging System Engineer" / "CDR Analyst", "RBT Operation Engineer" (Ringback Tone), "VAS Specialist", "Core Network Product Engineer", "IP Service Engineer", "Microwave Engineer", "Cloud Core Engineer", "NGBSS Test Engineer", "Mobile Money Maintenance Engineer", "Network Planning & Optimization" -> MUST be "technology-it", NEVER "engineering-construction"!
           - CRITICAL: "Core Banking System Admin" / "Banking IT Officer" / "Hospital IT Administrator" -> MUST be "technology-it", even if employed by a bank or hospital! Technical IT roles always take precedence over the employer's industry.

        5. ENGINEERING & CONSTRUCTION vs REAL ESTATE SALES & ACCOUNTING:
           - "Real Estate Project Manager" / "Real Estate Development and Project Manager" -> MUST be "engineering-construction", NOT "sales-marketing" (manages physical civil construction, contractors, and site delivery schedules, distinct from property sales brokers).
           - When a job is at an Engineering/Construction company or requires a Civil/Mechanical/Electrical Engineering degree:
             * "Monitoring and Reporting Division Head" -> "engineering-construction" (monitoring civil works and site progress, NOT financial reporting).
             * "Project Closing Division Head" -> "engineering-construction" (contractual close-out and commissioning of physical infrastructure, NOT financial year-end closing).
             * "Project Performance Audit Division Head" -> "engineering-construction" (civil engineering worksite performance auditing, NOT financial/tax auditing).
             * "Bid Management Division Head" / "Estimation Specialist" / "BOQ Specialist" / "Quantity Surveyor" -> "engineering-construction".
             * "Equipment Rental Division Head" / "Rental Officer" / "Concrete Pump Operator" / "Line Pump Operator" -> "engineering-construction".
           - Civil Engineer, Structural Engineer, Architect, Landscape Designer, Site Supervisor -> "engineering-construction".

        6. SOCIAL SCIENCE & COMMUNITY vs ACCOUNTING (The "Economics" & Humanitarian Trap):
           - In NGOs and humanitarian programs, "Economic Strengthening Specialist", "Livelihoods Specialist/Officer", "VSLA Facilitator", "Community Engagement Facilitator", "Reintegration Associate" -> MUST be "social-science-community", NOT "accounting-finance" (these are household food security, poverty alleviation, and community empowerment roles).
           - "GCR Advisor" (Global Compact on Refugees), Refugee Protection, Migration Advisor, Human Rights Officer -> "social-science-community" (or "legal" for legal counsel).
           - "MEAL" / "M&E" (Monitoring, Evaluation, Accountability, and Learning) Officers and Interns, "Data Collector" in NGOs -> MUST be "social-science-community", NOT "accounting-finance".

        7. HEALTHCARE & MEDICAL vs SCHOOLS & INDUSTRIAL QA:
           - Doctor, Medical Internist, General Practitioner, Anesthetist, Nurse, Home Care Nurse, Midwife, Pharmacist, Druggist -> MUST be "healthcare".
           - Medical health programs: "NCD Project Coordinator" (Non-Communicable Diseases), "RMNCH Officer" (Reproductive, Maternal, Newborn & Child Health), "HIV Prevention Officer", "HIV Service Officer" -> MUST be "healthcare", NEVER "engineering-construction" or "security-protection".
           - CRITICAL DISAMBIGUATION:
             * "Lab Technician" at a School, College, or International School -> MUST be "education-training", NOT "healthcare" (educational science lab supporting teachers, not clinical hospital diagnostics).
             * "Safety Health Environment & Quality Manager (SHEQM)" or "HSE Officer" at a factory/plant -> MUST be "manufacturing-production" or "engineering-construction", NOT "healthcare".

        8. AGRICULTURE & NATURAL SCIENCE:
           - "Green Development Expert", "Environmental Management Expert", "Agronomist", "Forestry Specialist", "Soil Scientist" -> MUST be "agriculture-natural-science", NOT "engineering-construction".

        9. WORKPLACE SAFETY & HSE vs SECURITY:
           - "Workplace Safety Officer", "Senior Environment and Workplace Safety Officer", "HSE Coordinator", "Environmental Health & Safety" on construction sites or factories -> MUST be "engineering-construction" (if on civil worksites) or "manufacturing-production" (factory shop floor), NEVER "security-protection".
           - "security-protection" is STRICTLY reserved for physical security guards ("ጥበቃ"), watchmen, bodyguards, and security escorts.

        10. PROCUREMENT & SUPPLY CHAIN vs LOGISTICS:
           - "Purchaser", "Purchasing Officer", "Senior Procurement", "Procurement Specialist", "Buyer", "Sourcing Officer", "Storekeeper", "Store Keeper", "Warehouse Materials Keeper" -> MUST be "procurement-supply-chain", NOT "logistics-transportation" or "sales-marketing".
           - "Tender Officer" / "Bidding Officer" for commercial products, lab equipment, or trading goods -> MUST be "procurement-supply-chain", NOT "engineering-construction".
           - Drivers (truck driver, mixer driver, light vehicle driver, motorbike delivery driver, courier), Fleet Officer, Dispatcher, Freight Forwarder, Customs Clearance -> "logistics-transportation".

        11. SKILLED & GENERAL LABOR:
           - "Battery Swapper" (electric vehicle battery swapping attendant), Tailor / Garment Maker ("የልብስ ስፌት"), Woodworker / Carpenter ("የእንጨት ሥራ"), Furniture Machinist, Ceramic Professional / Tiler ("የሴራሚክ ባለሙያ"), Welder, Mason, Janitor/Cleaner -> MUST be "skilled-general-labor".
           - "Automotive Electrical Technician" / "Auto Electrician" / "EV Service Technician" / "Auto Mechanic" -> MUST be "skilled-general-labor", NOT "engineering-construction".

        12. HOSPITALITY & TOURISM vs RETAIL:
           - Hotel management, chef, cook, waiter/waitress, front desk hotel, food court supervisor ("Food Court Supervisor and Marketing") -> MUST be "hospitality-tourism".
           - Commercial mall recreation: "የጌም ዞን" / Game Zone Section Head / Arcade & Entertainment Center Attendant -> MUST be "hospitality-tourism".

        13. BUSINESS & ADMINISTRATION vs EDUCATION / CLERICAL:
           - Secretaries, Executive Assistants, Office Administrators, Archive Officers, Team Assistants -> "business-administration".
           - CRITICAL: "TVET Secretary", "School Secretary", or "Team Assistant" (e.g. at World Bank or NGOs) is secretarial/clerical and belongs in "business-administration", NOT "education-training" or "accounting-finance".

        14. MEDIA & COMMUNICATION vs DESIGN & CREATIVE vs PROCUREMENT:
           - Video Editor, Videographer, Social Media Manager, Content Writer, Journalist, Communications Manager, Public Relations -> "media-communication".
           - Terms of Reference (TOR) or Consultancy for creating accessible communication, multimedia, or Braille materials -> MUST be "media-communication", NOT "procurement-supply-chain".
           - Graphic Designer, UI/UX Designer, Motion Graphics Artist, Creative Director -> "design-creative".

        15. ACCOUNTING & FINANCE (Role vs Employer Industry Trap):
           - "Accountant", "Senior Accountant", "Junior Accountant", "Cost Accountant", "Project Accountant", "Finance Officer", "Finance Manager", "Auditor", "Internal Auditor", "Cashier", "Disbursement Officer", "Tax Officer" -> MUST ALWAYS be "accounting-finance".
           - AMHARIC ACCOUNTING TITLES: "አካውንታንት", "ሲኒየር አካውንታንት", "ሲኒዬር አካውንታንት", "ጁኒየር አካውንታንት", "የሂሳብ ባለሙያ", "ሂሳብ ሹም", "ዋና ሂሳብ ሹም", "ገንዘብ ያዥ", "ካሸር", "ፋይናንስ ኦፊሰር" -> MUST ALWAYS be "accounting-finance".
           - CRITICAL TRAP: When an accountant or cashier works at a School, College, University, Training Institute, Factory, Hotel, Hospital, or NGO, their role is financial accounting, NOT teaching, healthcare, or hospitality! They MUST be classified as "accounting-finance".

        16. If none fit, set sectorSlug to null.
        17. Return ONLY a valid JSON object matching this schema:
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

        var batches = CreateTokenBudgetedBatches(jobs);
        _logger.LogInformation(
            "Batch of {Total} jobs dynamically packed into {Count} token-budgeted micro-batches (<= 3,800 tokens each) to respect rate limits.",
            jobs.Count, batches.Count);

        var aggregatedResults = new List<UnifiedJobClassificationResult>(jobs.Count);
        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var batchResults = await EvaluateChunkAsync(batch, sectors, sectorBySlug, systemPrompt, ct);
            aggregatedResults.AddRange(batchResults);

            if (i + 1 < batches.Count)
            {
                // Rate-limit pacing delay: allows provider token buckets (TPM) to replenish
                await Task.Delay(2000, ct);
            }
        }
        return aggregatedResults;
    }

    /// <summary>
    /// Dynamically packs jobs into token-budgeted batches (<= 3,800 tokens total)
    /// to strictly respect provider RPM/TPM ceilings (e.g. Groq 6,000 TPM limit).
    /// </summary>
    private static List<List<Job>> CreateTokenBudgetedBatches(
        IReadOnlyList<Job> jobs,
        int systemPromptTokens = 1750,
        int maxBatchTokens = 3800)
    {
        var batches = new List<List<Job>>();
        var currentBatch = new List<Job>();
        var currentBatchTokens = systemPromptTokens;

        foreach (var job in jobs)
        {
            var descSnippet = Truncate(job.Description, 250);
            var titleLen = job.Title?.Length ?? 0;
            var compLen = job.Company?.Length ?? 0;
            var locLen = job.Location?.Length ?? 0;
            var secLen = job.SourceSectors?.Length ?? 0;

            // Character to token approximation: chars / 3.6 + 60 output tokens + 25 json overhead
            var jobChars = titleLen + compLen + locLen + secLen + descSnippet.Length;
            var estimatedJobTokens = (int)Math.Ceiling(jobChars / 3.6) + 85;

            // If adding this job exceeds the ceiling, yield current batch and start new
            if (currentBatch.Count > 0 && (currentBatchTokens + estimatedJobTokens) > maxBatchTokens)
            {
                batches.Add(currentBatch);
                currentBatch = new List<Job>();
                currentBatchTokens = systemPromptTokens;
            }

            currentBatch.Add(job);
            currentBatchTokens += estimatedJobTokens;
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
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
            description = Truncate(j.Description, 250) // Reduced from 500 to 250 chars to save 40% token budget
        });

        var userPrompt = $$"""
        Classify the following {{chunk.Count}} job postings into canonical sectors:

        {{JsonSerializer.Serialize(userItems, new JsonSerializerOptions { WriteIndented = true })}}
        """;

        // ── TIER 1: OpenRouter (NVIDIA Nemotron 3 Ultra 550B / Llama 3.3 70B with Dual-Key Failover) ──
        if (_openRouter.IsConfigured)
        {
            try
            {
                _logger.LogInformation("Attempting Tier 1 (OpenRouter Nemotron 550B) classification for {Count} jobs.", chunk.Count);
                var openRouterEnvelope = await _openRouter.ChatJsonAsync<ClassificationEnvelope>(
                    systemPrompt, userPrompt, timeoutSeconds: 45, ct: ct);

                if (openRouterEnvelope?.Results is { Count: > 0 })
                {
                    return MapResults(chunk, openRouterEnvelope.Results, sectorBySlug, "OpenRouter Nemotron");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tier 1 (OpenRouter) classification failed or was rate-limited. Falling back to Tier 2 (Groq).");
                await RecordFallbackAsync("Tier 1 (OpenRouter Nemotron 550B)", "OpenRouter", ex, "Tier 2 (Groq Llama 3.3 70B)", chunk, ct);
            }
        }

        // ── TIER 2: Groq Cloud LLM (Meta Llama 3.3 70B with automatic 8B Instant fallback) ──
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
                await RecordFallbackAsync("Tier 2 (Groq Llama 3.3 70B)", "Groq", ex, "Tier 3 (Google Gemini 2.0 Flash)", chunk, ct);
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
                await RecordFallbackAsync("Tier 3 (Google Gemini 2.0 Flash)", "Gemini", ex, "Tier 4 (Ollama / Groq 8B)", chunk, ct);
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
            _logger.LogInformation("Tier 4 (Ollama) unavailable or failed: {Message}.", ex.Message);
        }

        // ── EMERGENCY HIGH-THROUGHPUT AI PASS (Groq Llama 3.1 8B Instant - 30,000 TPM) ──
        if (_groq.IsConfigured)
        {
            try
            {
                _logger.LogInformation("Attempting Emergency High-Throughput AI Pass (Groq Llama 3.1 8B Instant - 30k TPM) for {Count} jobs.", chunk.Count);
                var emergencyEnvelope = await _groq.ChatJsonAsync<ClassificationEnvelope>(
                    model: "llama-3.1-8b-instant",
                    systemPrompt: systemPrompt,
                    userPrompt: userPrompt,
                    timeoutSeconds: 20,
                    ct: ct);

                if (emergencyEnvelope?.Results is { Count: > 0 })
                {
                    return MapResults(chunk, emergencyEnvelope.Results, sectorBySlug, "Groq Llama 8B Instant");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Emergency Groq 8B Instant pass failed. Falling back to Safe Uncategorized Queue.");
                await RecordFallbackAsync("Emergency Groq 8B Instant", "Groq 8B", ex, "Safe Uncategorized Queue", chunk, ct);
            }
        }

        // ── TIER 5: Safe Uncategorized Fallback (Prevents Cosine Bag-of-Words Misclassifications) ─
        _logger.LogWarning("All AI tiers exhausted for {Count} jobs. Leaving Uncategorized for automated AI retry.", chunk.Count);
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
    /// Tier 5 Fallback: Finds nearest historical jobs by title-weighted semantic term overlap
    /// (cosine-normalized bag-of-words / TF-IDF) across already-classified jobs in the database.
    /// Title terms carry 5x weight, and company names are excluded to prevent false 100% matches.
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
            .Select(j => new { j.Id, j.Title, j.Description, j.Company, j.SectorId, j.SectorName })
            .ToListAsync(ct);

        var sectorById = activeSectors.ToDictionary(s => s.Id);

        foreach (var job in jobs)
        {
            var jobTokens = TokenizeJob(job.Title, job.Description, job.Company);
            double bestSimilarity = 0.0;
            Guid? bestSectorId = null;
            string? bestSectorName = null;

            foreach (var r in referenceJobs)
            {
                if (!r.SectorId.HasValue) continue;
                var refTokens = TokenizeJob(r.Title, r.Description, r.Company);
                var sim = ComputeCosineSimilarity(jobTokens, refTokens);

                if (sim > bestSimilarity)
                {
                    bestSimilarity = sim;
                    bestSectorId = r.SectorId.Value;
                    bestSectorName = r.SectorName;
                }
            }

            // SAFE FALLBACK: Never auto-assign a sector when LLMs are unavailable.
            // Retain job as Uncategorized so it can be re-evaluated by the next AI sync or manually reviewed.
            results.Add(new UnifiedJobClassificationResult(
                job.Id,
                null,
                null,
                null,
                0.0,
                "[AI Providers Busy] All AI tiers exhausted; safely left Uncategorized for automated AI retry",
                "Uncategorized",
                null));
        }

        return results;
    }

    private static Dictionary<string, double> TokenizeJob(string? title, string? description, string? company = null)
    {
        var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // Exclude company stop-words so different roles at the same company don't falsely match 100%
        var companyWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(company))
        {
            var cWords = company.ToLowerInvariant()
                .Split([' ', ',', '.', ':', ';', '(', ')', '[', ']', '-', '/', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var cw in cWords) if (cw.Length > 2) companyWords.Add(cw);
        }

        // Title terms carry 5.0x weight (the definitive sector signal)
        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleWords = title.ToLowerInvariant()
                .Split([' ', ',', '.', ':', ';', '(', ')', '[', ']', '-', '/', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var tw in titleWords)
            {
                if (tw.Length <= 2 || companyWords.Contains(tw)) continue;
                dict[tw] = dict.GetValueOrDefault(tw, 0.0) + 5.0;
            }
        }

        // Description terms carry 1.0x weight (secondary context)
        if (!string.IsNullOrWhiteSpace(description))
        {
            var descSnippet = Truncate(description, 150);
            var descWords = descSnippet.ToLowerInvariant()
                .Split([' ', ',', '.', ':', ';', '(', ')', '[', ']', '-', '/', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var dw in descWords)
            {
                if (dw.Length <= 3 || companyWords.Contains(dw)) continue;
                dict[dw] = dict.GetValueOrDefault(dw, 0.0) + 1.0;
            }
        }

        // L2 normalization
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

    private async Task RecordFallbackAsync(
        string attemptedTier,
        string failedProvider,
        Exception ex,
        string fallbackTier,
        IReadOnlyList<Job> chunk,
        CancellationToken ct = default)
    {
        var (statusCode, category, details) = CategorizeException(ex);
        var titleSnippet = string.Join(", ", chunk.Take(3).Select(j => j.Title));
        if (chunk.Count > 3) titleSnippet += $" (+{chunk.Count - 3} more)";

        try
        {
            var audit = new AiProviderAuditLog
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
                AttemptedTier = attemptedTier,
                FailedProvider = failedProvider,
                HttpStatusCode = statusCode,
                ErrorCategory = category,
                ErrorMessage = details,
                FallbackTier = fallbackTier,
                JobCount = chunk.Count,
                WasRecovered = true,
                JobTitlesSnippet = titleSnippet
            };

            _db.AiProviderAuditLogs.Add(audit);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception dbEx)
        {
            _logger.LogWarning(dbEx, "Failed to persist AiProviderAuditLog.");
        }

        // Send instant Telegram alert to admin channel/group if configured
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var telegram = scope.ServiceProvider.GetService<TelegramBotService>();
            if (telegram != null)
            {
                var codeStr = statusCode.HasValue ? $" ({statusCode.Value})" : "";
                var msg = $"⚠️ <b>AI Provider Fallback Event</b>\n\n" +
                          $"• <b>Attempted:</b> {attemptedTier}\n" +
                          $"• <b>Failed:</b> {failedProvider} — <i>{category}{codeStr}</i>\n" +
                          $"• <b>Reason:</b> <code>{details}</code>\n" +
                          $"• <b>Switched To:</b> {fallbackTier}\n" +
                          $"• <b>Jobs Affected:</b> {chunk.Count} jobs\n" +
                          $"• <b>Sample:</b> {titleSnippet}";
                await telegram.SendAdminAlertAsync(msg);
            }
        }
        catch (Exception tgEx)
        {
            _logger.LogDebug(tgEx, "Optional Telegram alert failed.");
        }
    }

    private static (int? StatusCode, string Category, string Details) CategorizeException(Exception ex)
    {
        var msg = ex.Message ?? "Unknown error";
        if (ex is OpenRouterRateLimitException rle)
        {
            return (rle.StatusCode, "RateLimit", $"OpenRouter rate-limited: {rle.Message}");
        }

        if (msg.Contains("429", StringComparison.OrdinalIgnoreCase) || msg.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || msg.Contains("quota", StringComparison.OrdinalIgnoreCase))
        {
            return (429, "RateLimit", Truncate(msg, 250));
        }

        if (ex is TaskCanceledException || ex is TimeoutException || msg.Contains("timed out", StringComparison.OrdinalIgnoreCase) || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return (408, "Timeout", Truncate(msg, 250));
        }

        if (msg.Contains("401", StringComparison.OrdinalIgnoreCase) || msg.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) || msg.Contains("invalid api key", StringComparison.OrdinalIgnoreCase))
        {
            return (401, "Auth", Truncate(msg, 250));
        }

        if (msg.Contains("503", StringComparison.OrdinalIgnoreCase) || msg.Contains("502", StringComparison.OrdinalIgnoreCase) || msg.Contains("overloaded", StringComparison.OrdinalIgnoreCase))
        {
            return (503, "ServerError", Truncate(msg, 250));
        }

        if (ex is System.Text.Json.JsonException || msg.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return (null, "MalformedJson", Truncate(msg, 250));
        }

        return (null, "Error", Truncate(msg, 250));
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
