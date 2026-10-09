using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SeraGo.API.Services;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

public static class ArchitectureDataEndpoint
{
    public static IEndpointRouteBuilder MapArchitectureDataEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/architecture/stats", GetArchitectureStatsAsync)
            .RequireRateLimiting("jobs_read")
            .WithTags("Architecture")
            .WithOpenApi();
        return app;
    }

    public sealed record ScraperRun(
        int api_hits,
        string time,
        int found,
        string stable_websites_status,
        int skipped,
        int updated,
        int inserted,
        string unstable_websites_status);

    public sealed record ScraperData(
        string Status,
        int RunCount,
        int TotalApiHits,
        int TotalItemsFound,
        int TotalItemsInserted,
        int TotalItemsUpdated,
        int TotalItemsSkipped,
        List<ScraperRun> RecentRuns);

    public sealed record AiClassification(
        string JobTitle,
        string SectorBefore,
        string SectorAfter,
        string Reasoning);

    public sealed record DatabaseData(
        int ActiveJobs,
        int TotalJobViews,
        int TotalUsers,
        int ViewsToday,
        int ViewsThisWeek,
        int ViewsThisMonth);

    public sealed record SectorPercent(
        string SectorName,
        double Percentage);

    public sealed record ArchitectureStatsResponse(
        ScraperData Scraper,
        List<AiClassification> RecentAiClassifications,
        DatabaseData Database,
        List<SectorPercent> UserSectors);

    private static async Task<IResult> GetArchitectureStatsAsync(
        ApplicationDbContext db,
        ScraperDbOptions scraperDb,
        AiClassificationDbOptions aiDb,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

        // 1. Database Data
        var todayStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, TimeSpan.FromHours(3)).ToUniversalTime();
        var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
        var monthStart = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.FromHours(3)).ToUniversalTime();

        var activeJobs = await db.Jobs.AsNoTracking().CountAsync(j => j.IsActive && j.Status == SeraGo.Core.Domain.Enums.JobStatus.Published, ct);
        var totalViews = await db.JobViews.AsNoTracking().CountAsync(ct);
        var totalUsers = await db.Users.AsNoTracking().CountAsync(ct);
        
        var viewsToday = await db.JobViews.AsNoTracking().CountAsync(v => v.ViewedAt >= todayStart, ct);
        var viewsThisWeek = await db.JobViews.AsNoTracking().CountAsync(v => v.ViewedAt >= weekStart, ct);
        var viewsThisMonth = await db.JobViews.AsNoTracking().CountAsync(v => v.ViewedAt >= monthStart, ct);

        var dbData = new DatabaseData(activeJobs, totalViews, totalUsers, viewsToday, viewsThisWeek, viewsThisMonth);

        // 2. User Sectors (parse settings JSON to find preferences)
        var allSettings = await db.UserSettings.AsNoTracking().Select(s => s.Settings).ToListAsync(ct);
        var sectorCounts = new Dictionary<Guid, int>();
        int totalPreferences = 0;

        foreach (var s in allSettings)
        {
            var ids = UserSettingsReader.GetForYouSectorIds(s);
            foreach (var id in ids)
            {
                if (!sectorCounts.ContainsKey(id)) sectorCounts[id] = 0;
                sectorCounts[id]++;
                totalPreferences++;
            }
        }

        var sectorPercents = new List<SectorPercent>();
        if (totalPreferences > 0)
        {
            var sectorIds = sectorCounts.Keys.ToList();
            var sectors = await db.Sectors.AsNoTracking()
                .Where(s => sectorIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

            foreach (var kvp in sectorCounts.OrderByDescending(c => c.Value).Take(6)) // Top 6 sectors
            {
                if (sectors.TryGetValue(kvp.Key, out var name))
                {
                    double percent = Math.Round((double)kvp.Value / totalPreferences * 100, 1);
                    sectorPercents.Add(new SectorPercent(name, percent));
                }
            }
        }

        // 3. Scraper Data
        var scraperData = await GetScraperTodayAsync(scraperDb, today, ct);

        // 4. AI Data
        var aiData = await GetRecentAiClassificationsAsync(aiDb, db, ct);

        return Results.Ok(new ArchitectureStatsResponse(scraperData, aiData, dbData, sectorPercents));
    }

    private sealed class RawScraperRun
    {
        public int run { get; set; }
        public int hits { get; set; }
        public string time { get; set; }
        public int found { get; set; }
        public string status { get; set; }
        public int skipped { get; set; }
        public int updated { get; set; }
        public int inserted { get; set; }
        public string unstable_status { get; set; }
    }

    private static async Task<ScraperData> GetScraperTodayAsync(ScraperDbOptions scraperDb, DateOnly today, CancellationToken ct)
    {
        if (!scraperDb.IsConfigured) return new ScraperData("unconfigured", 0, 0, 0, 0, 0, 0, []);

        try
        {
            await using var conn = new NpgsqlConnection(scraperDb.ConnectionString);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT status, run_count, api_hits, items_found, items_inserted, items_updated, items_skipped, runs FROM core_scrapelog WHERE day = @today";
            cmd.Parameters.AddWithValue("today", today);
            
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                var jsonStr = reader.IsDBNull(7) ? "[]" : reader.GetString(7);
                var rawRuns = JsonSerializer.Deserialize<List<RawScraperRun>>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
                
                var recentRuns = rawRuns
                    .OrderByDescending(r => r.run)
                    .Take(5)
                    .Select(r => new ScraperRun(r.hits, r.time, r.found, r.status, r.skipped, r.updated, r.inserted, r.unstable_status))
                    .ToList();

                return new ScraperData(
                    reader.GetString(0), 
                    reader.GetInt32(1), 
                    reader.GetInt32(2), 
                    reader.GetInt32(3), 
                    reader.GetInt32(4),
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    recentRuns);
            }

            // Fallback to ScrapeStat
            await using var statCmd = conn.CreateCommand();
            statCmd.CommandText = "SELECT run_count, items_found, items_inserted FROM core_scrapestat WHERE period_type = 'week' ORDER BY period_start DESC LIMIT 1";
            await using var statReader = await statCmd.ExecuteReaderAsync(ct);
            if (await statReader.ReadAsync(ct))
            {
                return new ScraperData("week_stats", statReader.GetInt32(0), 0, statReader.GetInt32(1), statReader.GetInt32(2), 0, 0, []);
            }

            return new ScraperData("no_data", 0, 0, 0, 0, 0, 0, []);
        }
        catch
        {
            return new ScraperData("error", 0, 0, 0, 0, 0, 0, []);
        }
    }

    private static async Task<List<AiClassification>> GetRecentAiClassificationsAsync(AiClassificationDbOptions aiDb, ApplicationDbContext db, CancellationToken ct)
    {
        if (!aiDb.IsConfigured) return [];

        try
        {
            await using var conn = new NpgsqlConnection(aiDb.ConnectionString);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT job_id, original_sector_name, sector_name, reasoning 
                FROM ai_service_aiclassificationlog 
                ORDER BY ai_classified_at DESC LIMIT 5";
            
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var results = new List<AiClassification>();
            var jobIds = new List<Guid>();
            
            var rawLogs = new List<(string JobIdStr, string Before, string After, string Reasoning)>();

            while (await reader.ReadAsync(ct))
            {
                var jobIdStr = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var before = reader.IsDBNull(1) ? "Uncategorized" : reader.GetString(1);
                var after = reader.IsDBNull(2) ? "Unknown" : reader.GetString(2);
                var reasoning = reader.IsDBNull(3) ? "No reasoning provided." : reader.GetString(3);
                
                rawLogs.Add((jobIdStr, before, after, reasoning));
                
                if (Guid.TryParse(jobIdStr, out var gid))
                {
                    jobIds.Add(gid);
                }
            }

            var jobTitles = new Dictionary<Guid, string>();
            if (jobIds.Count > 0)
            {
                var jobs = await db.Jobs.AsNoTracking()
                    .Where(j => jobIds.Contains(j.Id))
                    .Select(j => new { j.Id, j.Title })
                    .ToListAsync(ct);
                
                foreach (var j in jobs) jobTitles[j.Id] = j.Title;
            }

            foreach (var log in rawLogs)
            {
                string title = "Unknown Job";
                if (Guid.TryParse(log.JobIdStr, out var gid) && jobTitles.TryGetValue(gid, out var foundTitle))
                {
                    title = foundTitle;
                }
                results.Add(new AiClassification(title, log.Before, log.After, log.Reasoning));
            }

            return results;
        }
        catch
        {
            return [];
        }
    }
}
