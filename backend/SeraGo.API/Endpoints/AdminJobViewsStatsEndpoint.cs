using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Admin-only aggregate job-view analytics for the dedicated stats page:
///   GET /api/admin/jobs/views/stats
/// Returns period totals, views split by source and by sector (pie charts),
/// a views-per-day trend (last 30 days), a views-per-month trend (last 12
/// months) and the top jobs ranked by view count.
/// </summary>
public static class AdminJobViewsStatsEndpoint
{
    public static IEndpointRouteBuilder MapAdminJobViewsStatsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/jobs/views/stats", GetViewsStatsAsync)
            .RequireAuthorization(p => p.RequireRole("Admin"))
            .RequireRateLimiting("jobs_read")
            .WithTags("Stats (admin)")
            .WithOpenApi();
        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record NameValue(string Name, int Value);

    public sealed record DayValue(string Date, int Views);

    public sealed record TopJobItem(
        Guid Id, string Title, string Company, string? SourceName,
        string? SectorName, int ViewCount, string Status);

    public sealed record ViewsStatsResponse(
        int TotalViews,
        int ViewsToday, int ViewsThisWeek, int ViewsThisMonth, int ViewsThisYear,
        List<NameValue> BySource,
        List<NameValue> BySector,
        List<DayValue> PerDay,
        List<NameValue> PerMonth,
        List<TopJobItem> TopJobs);

    // ------------------------------------------------------------ Handler

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetViewsStatsAsync(
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Load the raw view rows once (view events are small — id, job id,
        // viewed-at) and aggregate in memory to keep a single round trip.
        var views = await db.JobViews.AsNoTracking()
            .Select(v => new { v.JobId, v.ViewedAt })
            .ToListAsync(ct);

        var totalViews = views.Count;
        var viewsToday = views.Count(v => v.ViewedAt >= now.UtcDateTime.Date);
        var viewsThisWeek = views.Count(v => v.ViewedAt >= now.AddDays(-7));
        var viewsThisMonth = views.Count(v => v.ViewedAt >= now.AddDays(-30));
        // Calendar year: from Jan 1 of the current year (UTC).
        var yearStart = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var viewsThisYear = views.Count(v => v.ViewedAt >= yearStart);

        // Job attributes for the pies / top list.
        var jobAttrs = await db.Jobs.AsNoTracking()
            .Select(j => new { j.Id, j.Title, j.Company, j.SourceName, j.SectorName, j.ViewCount, j.Status })
            .ToListAsync(ct);
        var attrById = jobAttrs.ToDictionary(j => j.Id);

        // Views by source (pie 1) — unknown job ids fall under "Unknown".
        var bySource = views
            .GroupBy(v => attrById.TryGetValue(v.JobId, out var a) ? (a.SourceName ?? "SeraGo") : "Unknown")
            .Select(g => new NameValue(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        // Views by sector (pie 2).
        var bySector = views
            .GroupBy(v => attrById.TryGetValue(v.JobId, out var a) ? (a.SectorName ?? "Uncategorized") : "Unknown")
            .Select(g => new NameValue(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        // Views per day — last 30 days, zero-filled so the trend has no gaps.
        var perDay = Enumerable.Range(0, 30)
            .Select(i => now.UtcDateTime.Date.AddDays(-i))
            .Reverse()
            .Select(day => new DayValue(
                day.ToString("yyyy-MM-dd"),
                views.Count(v => v.ViewedAt.Date == day)))
            .ToList();

        // Views per month — last 12 months, zero-filled.
        var perMonth = Enumerable.Range(0, 12)
            .Select(i => new DateTime(now.AddMonths(-i).Year, now.AddMonths(-i).Month, 1))
            .Reverse()
            .Select(month => new NameValue(
                month.ToString("yyyy-MM"),
                views.Count(v => v.ViewedAt.Year == month.Year && v.ViewedAt.Month == month.Month)))
            .ToList();

        // Top jobs by stored ViewCount (all jobs, not just viewed-in-window).
        var topJobs = jobAttrs
            .Where(j => j.ViewCount > 0)
            .OrderByDescending(j => j.ViewCount)
            .Take(10)
            .Select(j => new TopJobItem(
                j.Id, j.Title, j.Company, j.SourceName, j.SectorName, j.ViewCount,
                j.Status == JobStatus.Published ? "published"
                    : j.Status == JobStatus.PendingApproval ? "pendingApproval"
                    : j.Status == JobStatus.Draft ? "draft" : "rejected"))
            .ToList();

        return Results.Ok(new ViewsStatsResponse(
            totalViews,
            viewsToday, viewsThisWeek, viewsThisMonth, viewsThisYear,
            bySource, bySector, perDay, perMonth, topJobs));
    }
}
