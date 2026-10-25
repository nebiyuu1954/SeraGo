using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.API.Services;

/// <summary>
/// Turns a raw job (whatever the source calls its sector, or nothing at all)
/// into a canonical <see cref="Sector"/>. Lookup order:
///   1. SectorAliases — exact normalized match on the source's own sector name.
///   2. Title classifier — keyword rules (English + Amharic) on the job title.
///   3. null — unknown; the job is left uncategorized for admin review.
/// </summary>
public interface ISectorNormalizer
{
    Task<Sector?> NormalizeAsync(string? rawSector, string? title, CancellationToken ct = default);
}

public class SectorNormalizer(ApplicationDbContext db) : ISectorNormalizer
{
    public async Task<Sector?> NormalizeAsync(
        string? rawSector, string? title, CancellationToken ct = default)
    {
        // Load the vocabulary (small — a few dozen rows) once per call.
        var sectors = await db.Sectors
            .Where(s => s.IsActive)
            .Include(s => s.Aliases)
            .AsNoTracking()
            .ToListAsync(ct);

        // 1. Exact alias match on the source's sector name.
        if (!string.IsNullOrWhiteSpace(rawSector))
        {
            var normalized = SectorSeedData.Normalize(rawSector);
            var hit = sectors.FirstOrDefault(s =>
                s.Aliases.Any(a => a.Alias == normalized));
            if (hit is not null)
            {
                return hit;
            }
        }

        // 2. Title keyword classification (sources with no sector data).
        if (!string.IsNullOrWhiteSpace(title))
        {
            var slug = ClassifyTitle(title);
            if (slug is not null)
            {
                return sectors.FirstOrDefault(s => s.Slug == slug);
            }
        }

        return null;
    }

    /// <summary>
    /// Keyword rules, most specific first. Returns the matching sector slug or
    /// null. Rules are ordered so overlap resolves predictably — e.g.
    /// "software engineer" hits Technology (software) before Engineering
    /// (engineer), while "civil engineer" falls through to Engineering.
    /// </summary>
    public static string? ClassifyTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var text = title.ToLowerInvariant();

        foreach (var (slug, keywords) in Rules)
        {
            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword))
                {
                    return slug;
                }
            }
        }

        return null;
    }

    private static readonly (string Slug, string[] Keywords)[] Rules =
    [
        ("technology-it", ["software", "developer", "programmer", "frontend", "backend",
            "full stack", "flutter", "react", "angular", "vue", "devops", "cloud engineer",
            "platform engineer", "machine learning", "artificial intelligence", "data scientist", "data engineer",
            "data analyst", "data science", "database", "it support", "system administrator", "system admin",
            "core banking system admin", "network", "cybersecurity", "qa automation", "qa engineer", "software tester",
            "computer science", "computer engineer", "genai", "llm", "business intelligence",
            "bi developer", "mobile developer", "app developer", "web developer", "wordpress",
            "informatics", "data encoder", "it officer", "computer", "information technology",
            "hardware technician", "it technician", "computer technician", "network technician", "is trainee",
            "ocs", "rbt", "billing engineer", "telecom", "telecommunications", "core network", "microwave"]),
        ("healthcare", ["nurse", "doctor", "physician", "medical", "health", "pharmacist",
            "pharmacy", "laboratory", "lab technician", "nutritionist", "dentist", "midwife",
            "clinical", "hospital", "nursing", "hygiene", "sanitation", "internist", "anesthetist",
            "general practitioner", "home care nurse", "ncd", "rmnch", "ህክምና", "ነርስ", "ሐኪም", "ሳንቴሽን", "ሃይጂን"]),
        ("banking-insurance", ["banking", "bank", "insurance", "relationship manager",
            "branch manager", "branch business manager", "customer relationship", "financing and investment",
            "ifb", "loan officer", "credit officer", "credit analyst", "underwriter",
            "ባንክ", "መድን"]),
        ("procurement-supply-chain", ["procurement", "purchasing", "purchaser", "supply chain",
            "buyer", "sourcing", "storekeeper", "store keeper", "store man", "store officer",
            "ስቶር ኪፐር", "መጋዘን", "ግዥ", "ግዢ"]),
        ("sales-marketing", ["sales", "marketing", "promotion", "business development",
            "account manager", "merchandiser", "sdr", "sales development", "cold calling",
            "telemarketer", "bpo", "commercial officer", "real estate", "real estate agent",
            "sales consultant", "sales representative", "sales executive", "pre-sales", "ገበያ ጥናት",
            "ሽያጭ", "ግብይት", "ማርኬቲንግ"]),
        ("engineering-construction", ["engineer", "engineering", "civil", "mechanical",
            "electrical", "architect", "construction", "surveyor", "surveying", "site supervisor",
            "project coordinator", "sanitary", "architectural", "automotive", "maintenance engineer", "geologist",
            "workplace safety", "safety officer", "hse", "concrete pump", "line pump", "pump operator",
            "tender", "boq", "draftsman", "autocad",
            "ምህንድስና", "መሀንዲስ", "ኢንጂነር"]),
        ("accounting-finance", ["accountant", "accounting", "finance", "financial",
            "auditor", "audit", "tax", "bookkeeper", "cashier", "payroll", "budget",
            "ገንዘብ ሰብሳቢ", "ሂሳብ", "ኦዲተር", "ፋይናንስ", "አካውንታንት", "ካሸር", "ገንዘብ ያዥ"]),
        ("legal", ["lawyer", "legal", "attorney", "advocate", "compliance", "judge",
            "ህግ", "ጠበቃ"]),
        ("education-training", ["teacher", "teaching", "tutor", "instructor", "lecturer",
            "professor", "principal", "school", "education", "trainer", "mentor",
            "academic", "library", "መምህር", "አስተማሪ", "ማስተማር", "መምህርት"]),
        ("human-resources", ["human resource", "hr officer", "hr manager", "hr generalist", "recruiter",
            "recruitment", "talent acquisition", "ሰው ሃይል", "ሪክሩትመንት"]),
        ("logistics-transportation", ["driver", "transport", "logistics", "delivery",
            "courier", "dispatch", "truck", "vehicle", "fleet", "customs", "clearing",
            "ሹፌር", "ትራንስፖርት"]),
        ("media-communication", ["journalist", "reporter", "editor", "media", "writer",
            "content", "communication", "public relations", "broadcast", "photographer",
            "videographer", "social media", "ዘጋቢ", "ጋዜጠኛ"]),
        ("design-creative", ["designer", "graphic", "creative", "artist", "fashion",
            "illustrator", "ui/ux", "animation", "video editor", "creative director",
            "ዲዛይነር", "ንድፍ"]),
        ("customer-service-support", ["customer service", "call center", "customer support",
            "client support", "receptionist", "front desk", "help desk", "ደንበኛ"]),
        ("hospitality-tourism", ["cook", "chef", "waiter", "waitress", "hospitality",
            "hotel", "tourism", "bartender", "barista", "housekeeping", "ሼፍ",
            "አስተናጋጅ"]),
        ("manufacturing-production", ["manufacturing", "production", "factory",
            "machine operator", "assembly", "industrial", "quality control", "ፋብሪካ"]),
        ("agriculture-natural-science", ["agriculture", "agronomist", "farm", "biology",
            "biologist", "chemistry", "chemist", "physics", "mathematician", "science",
            "veterinary", "agro", "rangeland", "watershed", "ግብርና", "እርሻ"]),
        ("social-science-community", ["social worker", "sociology", "psychologist",
            "history", "community", "meal", "m&e", "monitoring and evaluation",
            "economic strengthening", "livelihood", "refugee", "reintegration", "migration",
            "data collector", "community engagement", "ማህበረሰብ"]),
        ("security-protection", ["security guard", "security supervisor", "security officer", "guard",
            "bodyguard", "surveillance", "patrol", "ጥበቃ", "ጠባቂ"]),
        ("business-administration", ["general manager", "business manager", "administration",
            "administrative", "executive secretary", "office management", "operations manager",
            "archive officer", "secretary", "ስራ አስኪያጅ", "ማኔጀር", "ጸሐፊ"]),
        ("skilled-general-labor", ["janitor", "cleaner", "plumber", "carpenter",
            "welder", "mason", "electrician", "laborer", "crane", "rigger", "crane rigger",
            "equipment operator", "forklift", "technician", "mechanic", "tailor", "battery swapper",
            "ceramic", "machinist", "የሴራሚክ", "ሴራሚክ", "የልብስ ስፌት", "ስፌት", "አናፂ", "የእንጨት ሥራ", "ሰራተኛ"]),
    ];
}
