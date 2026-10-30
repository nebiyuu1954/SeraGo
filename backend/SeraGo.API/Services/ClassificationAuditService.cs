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
            else if (m == "adminmanual" || m == "manual")
            {
                q = q.Where(j => string.IsNullOrEmpty(j.SourceName));
            }
            else if (m == "deepseek")
            {
                var matchingJobIds = await _db.JobClassificationRecords
                    .Where(r => r.ModelUsed.Contains("DeepSeek"))
                    .Select(r => r.JobId)
                    .Distinct()
                    .ToListAsync(ct);
                q = q.Where(j => matchingJobIds.Contains(j.Id));
            }
            else if (m == "openrouter" || m == "nemotron")
            {
                var matchingJobIds = await _db.JobClassificationRecords
                    .Where(r => r.ModelUsed.Contains("OpenRouter") || r.ModelUsed.Contains("Nemotron"))
                    .Select(r => r.JobId)
                    .Distinct()
                    .ToListAsync(ct);
                q = q.Where(j => matchingJobIds.Contains(j.Id));
            }
            else if (m == "groq")
            {
                var matchingJobIds = await _db.JobClassificationRecords
                    .Where(r => r.ModelUsed.Contains("Groq"))
                    .Select(r => r.JobId)
                    .Distinct()
                    .ToListAsync(ct);
                q = q.Where(j => matchingJobIds.Contains(j.Id));
            }
            else if (m == "gemini")
            {
                var matchingJobIds = await _db.JobClassificationRecords
                    .Where(r => r.ModelUsed.Contains("Gemini"))
                    .Select(r => r.JobId)
                    .Distinct()
                    .ToListAsync(ct);
                q = q.Where(j => matchingJobIds.Contains(j.Id));
            }
            else if (m == "ai")
            {
                q = q.Where(j => j.ClassificationId != null || !string.IsNullOrEmpty(j.SourceName));
            }
        }

        var total = await q.CountAsync(ct);

        // Fetch paginated jobs sorted newest classified/updated first
        var jobs = await q
            .OrderByDescending(j => j.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Fetch exact AI classification records for these jobs
        var jobIds = jobs.Select(j => j.Id).ToList();
        var records = await _db.JobClassificationRecords
            .AsNoTracking()
            .Where(r => jobIds.Contains(r.JobId))
            .OrderByDescending(r => r.ClassifiedAt)
            .ToListAsync(ct);

        var recordByJobId = records
            .GroupBy(r => r.JobId)
            .ToDictionary(g => g.Key, g => g.First());

        var items = new List<ClassificationHistoryItem>(jobs.Count);

        foreach (var j in jobs)
        {
            string itemMethod;
            string itemDetails;
            double? itemConfidence;
            string? itemReasoning;

            if (recordByJobId.TryGetValue(j.Id, out var rec))
            {
                itemMethod = rec.ModelUsed;
                itemDetails = rec.ModelTier;
                itemConfidence = rec.Confidence;
                itemReasoning = rec.Reasoning;
            }
            else
            {
                var deduction = DeduceMethod(j, aliasToSectorSlug);
                itemMethod = deduction.Method;
                itemDetails = deduction.Details;
                itemConfidence = deduction.Confidence;
                itemReasoning = deduction.Reasoning;
            }

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
                itemMethod,
                itemDetails,
                itemConfidence,
                itemReasoning,
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
           - "SDR (Sales Development Representative)" / "BPO cold calling" / "Sales rep" / "Sales Consultant" / "Merchant Acquisition Manager" -> MUST be "sales-marketing", NOT "technology-it".
           - "Digital Marketing Specialist" / "Digital Marketer" / "Performance Marketer" / "SEO Specialist" / "PPC Specialist" -> MUST be "sales-marketing" (commercial acquisition & advertising campaigns), distinct from pure social media content creation.
           - "Real Estate Project Manager" / "Real Estate Development and Project Manager" -> MUST be "engineering-construction", NOT "sales-marketing" (physical site construction & contractor delivery, distinct from sales brokers).
           - Bank branch operations & branch managers ("Branch Business Manager", "Branch Manager", "Customer Service Officer" at a bank) and credit/loan analysts ("Credit Officer", "Senior Credit Officer", "Relationship Manager-Credit") -> MUST be "banking-insurance", NOT "business-administration" or "accounting-finance".
           - Technical IT roles ("Core Banking System Admin", "Database Admin", "Cloud Engineer", "IT Officer", "OCS Billing Engineer", "RBT Operation", "VAS Specialist", "Telecom Network", "Data Analyst", "Data Scientist", "CTO") -> MUST be "technology-it", even if employed by a bank, hospital, NGO, or telecom operator.
           - Construction firm functional heads ("Monitoring and Reporting", "Project Closing", "Bid Management", "Project Performance Audit", "Site Engineer", "Office Engineer", "Quantity Surveyor", "MEP Technician") -> MUST be "engineering-construction", NOT "accounting-finance".
           - "Tender Officer" / "Tender Expert" / "Bidding Officer" for commercial products, lab equipment, or trading goods -> MUST be "procurement-supply-chain", NOT "engineering-construction".
           - Logistics & Fleet: Drivers ("Driver", "Light Vehicle Driver", truck driver), "Logistics Supervisor", "Import and Export Expert", "Customs Clearance" -> MUST be "logistics-transportation". Amharic: "ሹፌር", "አሽከርካሪ", "ቀላል ተሸከርካሪ አሽከርካሪ" -> MUST be "logistics-transportation".
           - Humanitarian livelihoods ("Programme Analyst", "Community Mobilizer", "Economic Strengthening Specialist", "Livelihoods", "GCR Refugee Advisor", "MEAL / M&E") in NGOs -> MUST be "social-science-community", NOT "accounting-finance" (ignore raw scraper tag "Economics").
           - Humanitarian Diplomacy & Advocacy: When a leadership or advisory role focuses on humanitarian policy, stakeholder relations, and refugee/protection advocacy (e.g. "Director, Humanitarian Diplomacy and Communication", "Advocacy and Policy Lead") -> MUST be "social-science-community". (If strictly media relations, journalism, and press releases -> "media-communication").
           - Health program roles ("NCD Project Coordinator", "RMNCH", "HIV Service Officer") and medical clinicians ("Doctor", "Internist", "Anesthetist", "Nurse", "Pharmacist") -> MUST be "healthcare". Amharic: "ነርስ", "ፋርማሲስት", "የጤና ባለሙያ" -> "healthcare".
           - Health Systems Strengthening ("HRH Officer / Human Resources for Health", "Health System Strengthening", "HMIS Officer", "Disease Surveillance") -> MUST be "healthcare", NEVER "human-resources" or "business-administration".
           - Human Resources: "HR Officer", "Human Resources Manager", "Talent Acquisition Specialist", "People & Culture Officer", "Payroll Specialist" -> MUST be "human-resources" (distinct from HRH public health or HR recruitment marketing).
           - Mental Health & Clinical Consultation ("Mental Health Advisor", "Mental Health Expert Consultant", "Psychologist", "Trauma Counselor") -> MUST be "healthcare", even when consulting for media organizations (e.g. BBC Media Action) or humanitarian aid groups.
           - Training Consultancies & Workshops ("Consultancy Services for Training", "Trauma-Informed Management Training", "Corporate Trainer", "Capacity Building Specialist", "TOT Specialist", "Online English Tutor", "Revit Instructor") -> MUST be "education-training". Structured instructional delivery and workshops belong in "education-training" regardless of the topic. Amharic: "መምህር", "አስተማሪ" -> "education-training".
           - "Lab Technician" in schools, academies, or international schools -> MUST be "education-training", NOT "healthcare" (educational science lab supporting teachers, not clinical hospital diagnostics).
           - "Safety Health Environment & Quality Manager (SHEQM)" in factories/plants -> MUST be "manufacturing-production", NOT "healthcare".
           - "Green Development Expert" in industrial parks/agro-industry -> MUST be "agriculture-natural-science", NOT "engineering-construction".
           - Farm Management & Field Machinery ("Farm Supervisor / የእርሻ ተቆጣጣሪ", "Tractor Operator / ትራክተር ኦፕሬተር") -> MUST be "agriculture-natural-science".
           - Worksite safety ("Workplace Safety Officer", "HSE Coordinator", "Environment and Safety") -> MUST be "engineering-construction" or "manufacturing-production", NEVER "security-protection" (which is strictly for guards, watchmen, and security supervisors).
           - Sourcing & purchasing ("Purchaser", "Senior Procurement", "Buyer", "Storekeeper / ስቶር ጠባቂ / ንብረት ክፍል") -> MUST be "procurement-supply-chain".
           - Manual crafts & trades ("Senior Electrician", "Industrial Welder", "Auto Mechanic / EV Technician", "Tailor / ሰፊ / የልብስ ስፌት", "Ceramic Tiler", "Woodworker", "Battery Swapper", "Packer / ጫኝና አውራጅ") -> MUST be "skilled-general-labor". Amharic: "ጽዳት", "የቢሮ ጽዳት" -> "skilled-general-labor".
           - C-Suite & Executive Leadership ("Chief Executive Officer (CEO)", "Chief Operating Officer (COO)", "General Manager / GM", "Managing Director", "Deputy General Manager", "Country Director") -> MUST be "business-administration", UNLESS specialized technical officers: "CTO / CIO" -> "technology-it", "CFO" -> "accounting-finance", "CMO" -> "sales-marketing", "Chief Medical Officer" -> "healthcare".
           - Clerical Data Entry vs Data Analyst ("Data Encoder", "Data Entry Clerk", "ዳታ ኢንኮደር", "Archive Officer", "Documentation Officer") -> MUST be "business-administration" (distinct from technical "Data Analyst" -> "technology-it").
           - Clerical & Liaison: "ጉዳይ አስፈፃሚ" / "ጉዳይ አስፈጻሚነት" (Administrative & Liaison Officer) -> MUST ALWAYS be "business-administration", regardless of employer industry! "TVET Secretary", "Executive Secretary", "Asset and General Service Officer", "Team Assistant", "ተላላኪ" -> MUST be "business-administration".
           - "Accountant" / "Cashier" / "Auditor" / Amharic ("አካውንታንት", "ሲኒየር አካውንታንት", "የሂሳብ ባለሙያ", "ካሸር", "ገንዘብ ያዥ") -> MUST ALWAYS be "accounting-finance", even if employed by a school, training institute, factory, or hospital.
           - "Graphic Designer" / "UI/UX Designer" / "Product Designer / Fine Arts / Design & Innovation Officer-Arts" -> MUST be "design-creative".
           - "OP-ED Editor", "Video Editor" / "Social Media Manager & Content Creator" / "Voice Talent for TTS Training" -> MUST be "media-communication".
           - Hybrid Customer Support vs Social Media ("Customer Support & Social Media Manager"): If primary duty is handling customer chat inquiries, complaints, and orders -> "customer-service-support"; if promotional content and growth campaigns -> "media-communication".
           - Terms of Reference (TOR) or Consultancy for creating accessible communication, multimedia, or Braille materials -> MUST be "media-communication", NOT "procurement-supply-chain".
           - Commercial mall recreation & retail: "የሱቅ ሱፐርቫይዘር" (Shop Supervisor) -> "sales-marketing"; "የጌም ዞን" / Game Zone / Mall Arcade / Food Court Supervisor -> "hospitality-tourism". Amharic: "አስተናጋጅ", "ባሪስታ" -> "hospitality-tourism".
           - "የገበያ ጥናትና ልማት መምሪያ ሥራ አስኪያጅ" / Market Research & Business Development -> MUST be "sales-marketing", regardless of employer.
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

        if (string.IsNullOrEmpty(job.SourceName))
        {
            return ("AdminManual", "Platform Admin Manual Assignment", 1.0, "Manual platform assignment");
        }

        // Check if raw scraper tags directly matched a known alias
        var rawTags = ParseJsonList(job.SourceSectors);
        rawTags.AddRange(ParseEthioCategories(job.SourceCategories));
        foreach (var tag in rawTags)
        {
            if (aliasToSector.TryGetValue(tag, out var matched))
            {
                if (string.Equals(matched.Name, job.SectorName, StringComparison.OrdinalIgnoreCase))
                {
                    return ("AliasRule", $"Scraper Direct Tag Match ('{tag}')", 0.98, $"Directly matched canonical sector alias '{tag}'");
                }
            }
        }

        // Historical scraped jobs prior to exact JobClassificationRecords logging
        return ("Historical Placement", "Historical Placement (Prior to Tier Logging)", 0.90, "Classified prior to detailed tier logging");
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
