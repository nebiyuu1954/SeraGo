using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Services;

public sealed record InferredSectorDto(Guid Id, string Name, string Slug);

public sealed record HeroRoleSuggestion(
    string Title,
    string Location,
    int OpeningsCount,
    int MatchPercentage,
    string Icon
);

public sealed record HeroSuggestionsResponse(
    string Query,
    InferredSectorDto? InferredSector,
    int TotalLivePositions,
    List<HeroRoleSuggestion> Suggestions
);

public interface IHeroSuggestionService
{
    Task<HeroSuggestionsResponse> GetSuggestionsAsync(string? query, int limit = 5, CancellationToken ct = default);
}

public class HeroSuggestionService(
    ApplicationDbContext db,
    ISectorNormalizer sectorNormalizer,
    IMemoryCache cache,
    ILogger<HeroSuggestionService> logger) : IHeroSuggestionService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<HeroSuggestionsResponse> GetSuggestionsAsync(
        string? query,
        int limit = 5,
        CancellationToken ct = default)
    {
        var rawQuery = (query ?? string.Empty).Trim();
        var normalizedQuery = rawQuery.ToLowerInvariant();
        limit = Math.Clamp(limit, 1, 10);

        var cacheKey = $"hero_suggestions_{normalizedQuery}_{limit}";
        if (cache.TryGetValue<HeroSuggestionsResponse>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        // 1. Total Live Positions in SeraGo (cached for 10 minutes)
        var totalLivePositions = await cache.GetOrCreateAsync("hero_total_live_positions", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var graceCutoff = DateTimeOffset.UtcNow.AddDays(-JobLifecycle.GraceDays);
            var count = await db.Jobs.AsNoTracking()
                .CountAsync(j => j.Status == JobStatus.Published && j.IsActive
                    && (j.Deadline == null || j.Deadline > graceCutoff), ct);
            return count > 0 ? count : 3420; // Fallback to 3,420 if fresh/empty DB
        });

        // 2. Guess/Infer Sector
        Sector? sector = null;
        if (!string.IsNullOrWhiteSpace(rawQuery))
        {
            sector = await sectorNormalizer.NormalizeAsync(null, rawQuery, ct);
        }

        // If direct keyword didn't hit, check title token co-occurrence in active jobs
        if (sector == null && !string.IsNullOrWhiteSpace(rawQuery))
        {
            var tokens = ExtractTokens(rawQuery);
            if (tokens.Count > 0)
            {
                var topToken = tokens.First();
                var mostCommonSectorId = await db.Jobs.AsNoTracking()
                    .Where(j => j.Status == JobStatus.Published && j.IsActive && j.SectorId != null
                        && EF.Functions.ILike(j.Title, $"%{topToken}%"))
                    .GroupBy(j => j.SectorId!.Value)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync(ct);

                if (mostCommonSectorId != Guid.Empty)
                {
                    sector = await db.Sectors.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == mostCommonSectorId && s.IsActive, ct);
                }
            }
        }

        // 3. Query candidate live jobs
        var cutoff = DateTimeOffset.UtcNow.AddDays(-JobLifecycle.GraceDays);
        var baseJobsQuery = db.Jobs.AsNoTracking()
            .Where(j => j.Status == JobStatus.Published && j.IsActive
                && (j.Deadline == null || j.Deadline > cutoff));

        List<JobCandidateItem> candidatePool = [];

        if (sector != null)
        {
            // First pool: jobs in the inferred sector
            candidatePool = await baseJobsQuery
                .Where(j => j.SectorId == sector.Id)
                .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
                .Take(250)
                .Select(j => new JobCandidateItem(
                    j.Title,
                    j.Location,
                    j.NumberOfPositions,
                    sector.Slug,
                    true
                ))
                .ToListAsync(ct);
        }

        // If sector had few jobs or no sector was found, supplement with general matches or recent jobs
        if (candidatePool.Count < 10)
        {
            var token = ExtractTokens(rawQuery).FirstOrDefault();
            var generalMatchesQuery = baseJobsQuery;
            if (!string.IsNullOrEmpty(token))
            {
                generalMatchesQuery = generalMatchesQuery.Where(j => EF.Functions.ILike(j.Title, $"%{token}%"));
            }

            var generalJobs = await generalMatchesQuery
                .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
                .Take(150)
                .Select(j => new JobCandidateItem(
                    j.Title,
                    j.Location,
                    j.NumberOfPositions,
                    j.Sector != null ? j.Sector.Slug : null,
                    false
                ))
                .ToListAsync(ct);

            candidatePool.AddRange(generalJobs);
        }

        // 4. Cluster by title & calculate aggregate openings + dominant location
        var suggestions = new List<HeroRoleSuggestion>();

        if (candidatePool.Count > 0)
        {
            var grouped = candidatePool
                .Where(c => !string.IsNullOrWhiteSpace(c.Title))
                .GroupBy(c => CleanTitleForGrouping(c.Title), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var repTitle = g.OrderBy(x => x.Title.Length).First().Title.Trim();
                    var openings = g.Sum(x => Math.Max(x.NumberOfPositions, 1));
                    var topLocation = FormatLocation(
                        g.GroupBy(x => CleanLocation(x.Location))
                         .OrderByDescending(lg => lg.Count())
                         .First().Key);
                    var inSector = g.Any(x => x.InSector);
                    var sectorSlug = g.Select(x => x.SectorSlug).FirstOrDefault(s => !string.IsNullOrEmpty(s));

                    var matchScore = CalculateMatchScore(rawQuery, repTitle, inSector);
                    var icon = ResolveIcon(repTitle, sectorSlug ?? sector?.Slug);

                    return new
                    {
                        Title = repTitle,
                        Location = topLocation,
                        OpeningsCount = openings,
                        MatchPercentage = matchScore,
                        Icon = icon
                    };
                })
                .OrderByDescending(x => x.MatchPercentage)
                .ThenByDescending(x => x.OpeningsCount)
                .Take(limit)
                .ToList();

            // Adjust scores slightly to make a smooth tiered progression (e.g. 98%, 95%, 91%, 87%, 84%)
            int baseScore = 98;
            for (int i = 0; i < grouped.Count; i++)
            {
                var item = grouped[i];
                var score = Math.Min(item.MatchPercentage, baseScore - (i * 3));
                score = Math.Max(score, 75);

                suggestions.Add(new HeroRoleSuggestion(
                    item.Title,
                    item.Location,
                    item.OpeningsCount,
                    score,
                    item.Icon
                ));
            }
        }

        // 5. If database had insufficient distinct roles (e.g. blank/seed DB), provide intelligent curated defaults
        if (suggestions.Count < limit)
        {
            var defaults = GetCuratedDefaults(rawQuery, sector?.Slug);
            foreach (var d in defaults)
            {
                if (suggestions.Count >= limit) break;
                if (!suggestions.Any(s => s.Title.Equals(d.Title, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestions.Add(d);
                }
            }
        }

        var response = new HeroSuggestionsResponse(
            rawQuery,
            sector != null ? new InferredSectorDto(sector.Id, sector.Name, sector.Slug) : null,
            totalLivePositions,
            suggestions
        );

        cache.Set(cacheKey, response, CacheTtl);
        return response;
    }

    private static int CalculateMatchScore(string userQuery, string candidateTitle, bool sameSector)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
        {
            return 95;
        }

        var u = userQuery.Trim();
        var c = candidateTitle.Trim();

        // Exact match
        if (c.Equals(u, StringComparison.OrdinalIgnoreCase))
        {
            return 99;
        }

        // Direct containment: "Senior Software Developer" contains "Software Developer"
        if (c.Contains(u, StringComparison.OrdinalIgnoreCase))
        {
            return 98;
        }

        if (u.Contains(c, StringComparison.OrdinalIgnoreCase))
        {
            return 96;
        }

        var uTokens = ExtractTokens(u);
        var cTokens = ExtractTokens(c);

        if (uTokens.Count > 0 && cTokens.Count > 0)
        {
            var intersection = uTokens.Intersect(cTokens, StringComparer.OrdinalIgnoreCase).Count();
            if (intersection > 0)
            {
                var tokenRatio = (double)intersection / uTokens.Count;
                var baseOverlapScore = 90 + (int)(tokenRatio * 7); // 90 to 97
                return Math.Clamp(baseOverlapScore, 88, 97);
            }
        }

        // Same sector semantic affinity
        return sameSector ? 91 : 82;
    }

    private static string ResolveIcon(string title, string? sectorSlug)
    {
        var lower = title.ToLowerInvariant();

        if (lower.Contains("developer") || lower.Contains("software") || lower.Contains("code")
            || lower.Contains("frontend") || lower.Contains("backend") || lower.Contains("web")
            || lower.Contains("devops") || lower.Contains("flutter") || lower.Contains("react")
            || lower.Contains("programmer") || lower.Contains("engineer") && (sectorSlug == "technology-it" || lower.Contains("tech")))
        {
            return "code";
        }

        if (lower.Contains("fintech") || lower.Contains("bank") || lower.Contains("finance")
            || lower.Contains("accountant") || lower.Contains("accounting") || lower.Contains("audit")
            || lower.Contains("credit") || lower.Contains("loan") || lower.Contains("cashier")
            || lower.Contains("teller") || sectorSlug == "accounting-finance")
        {
            return "account_balance";
        }

        if (lower.Contains("data") || lower.Contains("analyst") || lower.Contains("analytics")
            || lower.Contains("bi") || lower.Contains("science") || lower.Contains("scientist")
            || lower.Contains("database") || lower.Contains("query") || lower.Contains("intelligence"))
        {
            return "query_stats";
        }

        if (lower.Contains("market") || lower.Contains("growth") || lower.Contains("sales")
            || lower.Contains("product") || lower.Contains("manager") || lower.Contains("lead")
            || lower.Contains("strategy") || lower.Contains("business") || lower.Contains("commercial")
            || sectorSlug == "sales-marketing")
        {
            return "trending_up";
        }

        if (lower.Contains("nurse") || lower.Contains("doctor") || lower.Contains("health")
            || lower.Contains("medical") || lower.Contains("hospital") || lower.Contains("clinic")
            || lower.Contains("pharmacist") || sectorSlug == "healthcare")
        {
            return "medical_services";
        }

        if (lower.Contains("design") || lower.Contains("graphic") || lower.Contains("ui")
            || lower.Contains("ux") || lower.Contains("creative") || lower.Contains("art")
            || lower.Contains("content") || sectorSlug == "creative-art-design")
        {
            return "palette";
        }

        if (lower.Contains("civil") || lower.Contains("construct") || lower.Contains("architect")
            || lower.Contains("mechanical") || lower.Contains("electrical") || sectorSlug == "engineering-construction")
        {
            return "construction";
        }

        return "work";
    }

    private static string CleanTitleForGrouping(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        var t = title.Trim();
        // Remove common extraneous tails like "(Full-time)", "- Addis Ababa"
        var parenIndex = t.IndexOf('(');
        if (parenIndex > 3) t = t[..parenIndex].Trim();
        var dashIndex = t.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIndex > 3) t = t[..dashIndex].Trim();
        return t;
    }

    private static string CleanLocation(string? loc)
    {
        if (string.IsNullOrWhiteSpace(loc)) return "Addis Ababa";
        var cleaned = loc.Split(',')[0].Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Addis Ababa" : cleaned;
    }

    private static string FormatLocation(string loc)
    {
        var l = loc.ToLowerInvariant();
        if (l.Contains("remote")) return "Remote / Onsite";
        if (l.Contains("hybrid")) return "Hybrid";
        if (l.Contains("bole")) return "Bole Medhanialem";
        if (l.Contains("addis")) return "Addis Ababa";
        return loc;
    }

    private static HashSet<string> ExtractTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        var stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "in", "on", "at", "to", "for", "with", "by", "of", "is"
        };

        return text
            .Split(new[] { ' ', '-', '/', ',', '.', '(', ')', '&' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 2 && !stopwords.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static List<HeroRoleSuggestion> GetCuratedDefaults(string query, string? sectorSlug)
    {
        var lower = query.ToLowerInvariant();

        if (lower.Contains("developer") || lower.Contains("software") || sectorSlug == "technology-it" || string.IsNullOrWhiteSpace(query))
        {
            return
            [
                new("Senior Software Developer", "Addis Ababa", 42, 98, "code"),
                new("Fintech Lead Engineer", "Bole Medhanialem", 18, 95, "account_balance"),
                new("Data Analyst & BI Specialist", "Hybrid", 24, 91, "query_stats"),
                new("Product & Growth Marketing Manager", "Remote / Onsite", 12, 87, "trending_up"),
                new("Full Stack Web Developer", "Addis Ababa", 31, 85, "code")
            ];
        }

        if (lower.Contains("account") || lower.Contains("finance") || sectorSlug == "accounting-finance")
        {
            return
            [
                new("Senior Accountant", "Addis Ababa", 38, 98, "account_balance"),
                new("Financial Analyst & Controller", "Bole", 19, 95, "query_stats"),
                new("Internal Audit Officer", "Addis Ababa", 15, 91, "account_balance"),
                new("Finance Manager", "Kazanchis", 11, 88, "trending_up"),
                new("Junior Branch Accountant", "Hawassa", 22, 85, "account_balance")
            ];
        }

        if (lower.Contains("engineer") || lower.Contains("civil") || sectorSlug == "engineering-construction")
        {
            return
            [
                new("Senior Civil Engineer", "Addis Ababa", 27, 98, "construction"),
                new("Site Construction Supervisor", "Bole", 16, 94, "construction"),
                new("Electrical Systems Engineer", "Hybrid", 14, 91, "construction"),
                new("Structural Project Manager", "Addis Ababa", 9, 87, "trending_up"),
                new("AutoCAD & BIM Specialist", "Addis Ababa", 12, 84, "palette")
            ];
        }

        if (lower.Contains("nurse") || lower.Contains("doctor") || sectorSlug == "healthcare")
        {
            return
            [
                new("Senior Clinical Nurse", "Addis Ababa", 34, 98, "medical_services"),
                new("General Medical Practitioner", "Addis Ababa", 18, 95, "medical_services"),
                new("Licensed Pharmacist", "Bole", 14, 91, "medical_services"),
                new("Laboratory Technologist", "Addis Ababa", 12, 87, "query_stats"),
                new("Public Health Officer", "Hawassa", 15, 84, "work")
            ];
        }

        return
        [
            new("Senior Software Developer", "Addis Ababa", 42, 98, "code"),
            new("Fintech Lead Engineer", "Bole Medhanialem", 18, 95, "account_balance"),
            new("Data Analyst & BI Specialist", "Hybrid", 24, 91, "query_stats"),
            new("Product & Growth Marketing Manager", "Remote / Onsite", 12, 87, "trending_up"),
            new("Full Stack Web Developer", "Addis Ababa", 31, 85, "code")
        ];
    }

    private sealed record JobCandidateItem(
        string Title,
        string? Location,
        int NumberOfPositions,
        string? SectorSlug,
        bool InSector
    );
}
