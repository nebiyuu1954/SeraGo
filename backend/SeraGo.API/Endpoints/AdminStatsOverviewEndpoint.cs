using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SeraGo.API.Services;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Rich admin dashboard overview — one call bundles every KPI the
/// Overview page needs:
///
///   GET /api/admin/stats/overview (admin)
///
/// Users (total, active, new-this-week, new-today, by-role),
/// scraper health (today's scrape per-site, api hits, runs, errors),
/// jobs (total, pending approval with quick-approve data),
/// applications + views.
/// </summary>
public static class AdminStatsOverviewEndpoint
{
    public static IEndpointRouteBuilder MapAdminStatsOverviewEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/stats/overview", GetOverviewAsync)
            .RequireAuthorization(p => p.RequireRole("Admin"))
            .RequireRateLimiting("jobs_read")
            .WithTags("Stats (admin)")
            .WithOpenApi();
        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record UserStats(
        int Total,
        int ActiveToday, int ActiveThisWeek, int ActiveThisMonth,
        int NewToday, int NewThisWeek, int NewThisMonth,
        RoleBreakdown ByRole);

    public sealed record RoleBreakdown(int Talent, int Recruiter, int Admin);

    public sealed record ScraperSiteStats(
        string Source, string Name, string Status,
        int RunCount, int ApiHits,
        int ItemsFound, int ItemsInserted, int ItemsUpdated, int ItemsSkipped);

    public sealed record ScraperTodayStats(
        DateTime? Day, string Status,
        int TotalRunCount, int TotalApiHits,
        int TotalItemsFound, int TotalItemsInserted, int TotalItemsUpdated, int TotalItemsSkipped,
        int SitesScraped, List<ScraperSiteStats> Sites);

    public sealed record PendingJobItem(
        Guid Id, string Title, string Company, string? SourceName,
        string? SectorName, string PostedByName, string CreatedAt);

    public sealed record JobStats(
        int Total, int Published, int PendingApproval, int Drafts, int Rejected,
        int TotalViews, int SeragoJobs,
        int ViewsToday, int ViewsThisWeek, int ViewsThisMonth, int ViewsThisYear,
        List<PendingJobItem> PendingJobs);

    public sealed record ApplicationStats(
        int Total, int Pending, int Reviewed, int Shortlisted,
        int Interview, int Hired, int Rejected);

    public sealed record LastSyncInfo(
        DateTime? RanAt, int Inserted, int Updated, int Unchanged,
        int Uncategorized, int Deactivated, bool SchedulerEnabled);

    public sealed record StatsOverviewResponse(
        UserStats Users, ScraperTodayStats Scraper, JobStats Jobs,
        ApplicationStats Applications, LastSyncInfo LastSync);

    // -------------------------------------------------------------- Handlers

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetOverviewAsync(
        ApplicationDbContext db,
        ScraperDbOptions scraperDb,
        SyncOptions syncOptions,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var weekAgo = now.AddDays(-7);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)); // Addis Ababa time

        // ---- Users ----
        var users = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.UserType, u.CreatedAt })
            .ToListAsync(ct);

        var totalUsers = users.Count;
        var newThisWeek = users.Count(u => u.CreatedAt >= weekAgo);
        var newToday = users.Count(u => u.CreatedAt.Date == now.Date);
        var monthAgo = now.AddDays(-30);
        var newThisMonth = users.Count(u => u.CreatedAt >= monthAgo);

        // Active users = users with at least one JobView or JobApplication in the period.
        // Cutoffs must be UTC — Npgsql rejects non-zero offsets on timestamptz params.
        var activeToday = await GetActiveUserCountAsync(db, now.UtcDateTime.Date, ct);
        var activeThisWeek = await GetActiveUserCountAsync(db, weekAgo, ct);
        var activeThisMonth = await GetActiveUserCountAsync(db, monthAgo, ct);

        var talentCount = users.Count(u => u.UserType == UserType.Talent);
        var recruiterCount = users.Count(u => u.UserType == UserType.Recruiter);
        var adminCount = users.Count(u => u.UserType == UserType.Admin);

        // ---- Jobs ----
        var jobCounts = await db.Jobs.AsNoTracking()
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        var totalViews = await db.JobViews.AsNoTracking().CountAsync(ct);

        // View period totals (rolling windows matching the users' active stats).
        // All cutoffs forced to UTC — Npgsql rejects non-zero offsets on
        // timestamptz parameters (the machine runs at +03:00).
        var viewsToday = await db.JobViews.AsNoTracking()
            .CountAsync(v => v.ViewedAt >= new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero), ct);
        var viewsThisWeek = await db.JobViews.AsNoTracking()
            .CountAsync(v => v.ViewedAt >= weekAgo, ct);
        var viewsThisMonth = await db.JobViews.AsNoTracking()
            .CountAsync(v => v.ViewedAt >= monthAgo, ct);
        // Calendar year: from Jan 1 of the current year (UTC), not a rolling window.
        var yearStart = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var viewsThisYear = await db.JobViews.AsNoTracking()
            .CountAsync(v => v.ViewedAt >= yearStart, ct);

        var seragoJobs = await db.Jobs.AsNoTracking()
            .Where(j => j.SourceName == null)
            .CountAsync(ct);

        // Pending jobs with poster name for quick approve/deny
        var pendingJobs = await db.Jobs.AsNoTracking()
            .Include(j => j.PostedBy)
            .Where(j => j.Status == JobStatus.PendingApproval)
            .OrderByDescending(j => j.CreatedAt)
            .Take(20)
            .Select(j => new PendingJobItem(
                j.Id, j.Title, j.Company,
                j.SourceName, j.SectorName,
                j.PostedBy.FirstName + " " + j.PostedBy.LastName,
                j.CreatedAt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")))
            .ToListAsync(ct);

        // ---- Applications ----
        var appCounts = await db.JobApplications.AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        // ---- Scraper today (from scraper DB) ----
        var scraperToday = await GetScraperTodayAsync(scraperDb, today, ct);

        // ---- Last sync ----
        var lastRun = await db.SyncRuns.AsNoTracking()
            .OrderByDescending(r => r.RanAt)
            .FirstOrDefaultAsync(ct);

        return Results.Ok(new StatsOverviewResponse(
            Users: new UserStats(
                totalUsers,
                activeToday, activeThisWeek, activeThisMonth,
                newToday, newThisWeek, newThisMonth,
                new RoleBreakdown(talentCount, recruiterCount, adminCount)),
            Scraper: scraperToday,
            Jobs: new JobStats(
                jobCounts.Values.Sum(),
                jobCounts.GetValueOrDefault(JobStatus.Published),
                jobCounts.GetValueOrDefault(JobStatus.PendingApproval),
                jobCounts.GetValueOrDefault(JobStatus.Draft),
                jobCounts.GetValueOrDefault(JobStatus.Rejected),
                totalViews,
                seragoJobs,
                viewsToday, viewsThisWeek, viewsThisMonth, viewsThisYear,
                pendingJobs),
            Applications: new ApplicationStats(
                appCounts.Values.Sum(),
                appCounts.GetValueOrDefault(ApplicationStatus.Pending),
                appCounts.GetValueOrDefault(ApplicationStatus.Reviewed),
                appCounts.GetValueOrDefault(ApplicationStatus.Shortlisted),
                appCounts.GetValueOrDefault(ApplicationStatus.Interview),
                appCounts.GetValueOrDefault(ApplicationStatus.Hired),
                appCounts.GetValueOrDefault(ApplicationStatus.Rejected)),
            LastSync: new LastSyncInfo(
                lastRun?.RanAt,
                lastRun?.Inserted ?? 0,
                lastRun?.Updated ?? 0,
                lastRun?.Unchanged ?? 0,
                lastRun?.Uncategorized ?? 0,
                lastRun?.Deactivated ?? 0,
                syncOptions.Enabled)));
    }

    /// <summary>
    /// Read today's scrape data from the scraper DB's ScrapeLog (master) table.
    /// Falls back to ScrapeStat if the day log doesn't exist yet.
    /// </summary>
    private static async Task<ScraperTodayStats> GetScraperTodayAsync(
        ScraperDbOptions scraperDb, DateOnly today, CancellationToken ct)
    {
        if (!scraperDb.IsConfigured)
        {
            return new ScraperTodayStats(null, "unconfigured", 0, 0, 0, 0, 0, 0, 0, []);
        }

        try
        {
            await using var conn = new NpgsqlConnection(scraperDb.ConnectionString);
            await conn.OpenAsync(ct);

            // Read today's master ScrapeLog
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT day, status, run_count, api_hits,
                       items_found, items_inserted, items_updated, items_skipped,
                       websites
                FROM core_scrapelog
                WHERE day = @today
                """;
            cmd.Parameters.AddWithValue("today", today);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
            {
                // No log for today yet — try the latest ScrapeStat
                return await GetLatestScraperStatAsync(conn, ct);
            }

            var day = reader.GetDateTime(0);
            var status = reader.GetString(1);
            var runCount = reader.GetInt32(2);
            var apiHits = reader.GetInt32(3);
            var itemsFound = reader.GetInt32(4);
            var itemsInserted = reader.GetInt32(5);
            var itemsUpdated = reader.GetInt32(6);
            var itemsSkipped = reader.GetInt32(7);
            var websitesJson = reader.GetString(8);

            var sites = ParseWebsitesJson(websitesJson);

            return new ScraperTodayStats(
                day, status, runCount, apiHits,
                itemsFound, itemsInserted, itemsUpdated, itemsSkipped,
                sites.Count, sites);
        }
        catch
        {
            // Scraper DB unreachable — return empty, don't crash the dashboard
            return new ScraperTodayStats(null, "unreachable", 0, 0, 0, 0, 0, 0, 0, []);
        }
    }

    private static async Task<ScraperTodayStats> GetLatestScraperStatAsync(
        NpgsqlConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT period_start, period_end, run_count, api_hits,
                   items_found, items_inserted, items_updated, items_skipped,
                   by_source
            FROM core_scrapestat
            WHERE period_type = 'week'
            ORDER BY period_start DESC
            LIMIT 1
            """;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return new ScraperTodayStats(null, "no_data", 0, 0, 0, 0, 0, 0, 0, []);
        }

        var periodEnd = reader.GetFieldValue<DateOnly>(1);
        var runCount = reader.GetInt32(2);
        var apiHits = reader.GetInt32(3);
        var itemsFound = reader.GetInt32(4);
        var itemsInserted = reader.GetInt32(5);
        var itemsUpdated = reader.GetInt32(6);
        var itemsSkipped = reader.GetInt32(7);
        var bySourceJson = reader.GetString(8);

        var sites = ParseBySourceJson(bySourceJson);

        return new ScraperTodayStats(
            periodEnd.ToDateTime(TimeOnly.MinValue), "week_stats",
            runCount, apiHits,
            itemsFound, itemsInserted, itemsUpdated, itemsSkipped,
            sites.Count, sites);
    }

    /// <summary>
    /// Parse the ScrapeLog.websites JSON array into per-site stats.
    /// Shape: [{"source":"afriwork","name":"Afriwork","status":"success","run_count":2,"api_hits":10,"items_found":50,...}]
    /// </summary>
    private static List<ScraperSiteStats> ParseWebsitesJson(string json)
    {
        var result = new List<ScraperSiteStats>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                result.Add(new ScraperSiteStats(
                    GetString(item, "source"),
                    GetString(item, "name"),
                    GetString(item, "status"),
                    GetInt(item, "run_count"),
                    GetInt(item, "api_hits"),
                    GetInt(item, "items_found"),
                    GetInt(item, "items_inserted"),
                    GetInt(item, "items_updated"),
                    GetInt(item, "items_skipped")));
            }
        }
        catch { /* malformed JSON — return what we have */ }

        return result;
    }

    /// <summary>
    /// Parse the ScrapeStat.by_source JSON object into per-site stats.
    /// Shape: {"afriwork": {"items_found": 50, "items_inserted": 45, "run_count": 2, "api_hits": 10}}
    /// </summary>
    private static List<ScraperSiteStats> ParseBySourceJson(string json)
    {
        var result = new List<ScraperSiteStats>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var src = prop.Value;
                result.Add(new ScraperSiteStats(
                    prop.Name,
                    prop.Name, // display name not in this format, use slug
                    "success", // aggregate = all successful
                    GetInt(src, "run_count"),
                    GetInt(src, "api_hits"),
                    GetInt(src, "items_found"),
                    GetInt(src, "items_inserted"),
                    GetInt(src, "items_updated"),
                    GetInt(src, "items_skipped")));
            }
        }
        catch { /* malformed JSON */ }

        return result;
    }

    private static string GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : name;

    private static int GetInt(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    /// <summary>
    /// Counts distinct users who have at least one JobView or JobApplication
    /// on or after the given cutoff (UTC midnight).
    /// Npgsql only accepts UTC DateTimeOffset for timestamptz columns, so we
    /// normalise the cutoff to Utc before passing it to the query.
    /// </summary>
    private static async Task<int> GetActiveUserCountAsync(
        ApplicationDbContext db, DateTimeOffset cutoff, CancellationToken ct)
    {
        // Force UTC — Npgsql rejects non-zero offsets on timestamptz parameters.
        var utcCutoff = new DateTimeOffset(cutoff.UtcDateTime, TimeSpan.Zero);

        // Job views with a UserId (authenticated visitors).
        var viewIds = await db.JobViews.AsNoTracking()
            .Where(v => v.UserId != null && v.ViewedAt >= utcCutoff)
            .Select(v => v.UserId!)
            .Distinct()
            .ToListAsync(ct);

        // Job applications by users in the period.
        var appIds = await db.JobApplications.AsNoTracking()
            .Where(a => a.CreatedAt >= utcCutoff)
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync(ct);

        // Union of both sets.
        return viewIds.Union(appIds).Count();
    }
}
