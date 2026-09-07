using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Admin-only job analytics endpoints:
///   GET /api/admin/jobs/views — paginated list of jobs sorted by view count,
///     with source and view count, filterable by source and sort direction.
/// </summary>
public static class AdminJobStatsEndpoints
{
    public static IEndpointRouteBuilder MapAdminJobStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/jobs/views", GetJobsByViewsAsync)
            .RequireAuthorization(p => p.RequireRole("Admin"))
            .RequireRateLimiting("jobs_read")
            .WithTags("Stats (admin)")
            .WithOpenApi();
        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed class JobViewsQuery
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? Source { get; set; }       // exact source name filter, e.g. "Afriwork"
        public string? Sort { get; set; }         // "views_desc" | "views_asc" | "newest" | "oldest"
    }

    public sealed record JobViewItem(
        Guid Id, string Title, string Company, string? Location,
        string? SourceName, int ViewCount, string Status,
        string CreatedAt, string? PublishedAt, string? Deadline,
        string? SectorName, string? Salary);

    public sealed record JobViewsData(
        List<JobViewItem> Items,
        int TotalCount,
        int Page, int PageSize, int TotalPages, bool HasNextPage);

    // ------------------------------------------------------------- Handler

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetJobsByViewsAsync(
        [AsParameters] JobViewsQuery query,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        const int maxPageSize = 50;
        var page = Math.Clamp(query.Page ?? 1, 1, 100_000);
        var pageSize = Math.Clamp(query.PageSize ?? 10, 1, maxPageSize);

        var q = db.Jobs.AsNoTracking()
            .Include(j => j.Sector)
            .Where(j => j.IsActive || j.Status == JobStatus.PendingApproval || j.Status == JobStatus.Draft);

        // Source filter (exact match on SourceName).
        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            q = q.Where(j => j.SourceName == query.Source.Trim());
        }

        // Total count for pagination (before sorting/paging).
        var totalCount = await q.CountAsync(ct);

        // Sorting.
        q = query.Sort?.ToLowerInvariant() switch
        {
            "views_desc" => q.OrderByDescending(j => j.ViewCount).ThenByDescending(j => j.CreatedAt),
            "views_asc"  => q.OrderBy(j => j.ViewCount).ThenByDescending(j => j.CreatedAt),
            "oldest"     => q.OrderBy(j => j.CreatedAt),
            _           => q.OrderByDescending(j => j.ViewCount).ThenByDescending(j => j.CreatedAt), // default: top views
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id, j.Title, j.Company, j.Location,
                j.SourceName, j.ViewCount,
                Status = j.Status == JobStatus.Published ? "published"
                    : j.Status == JobStatus.PendingApproval ? "pendingApproval"
                    : j.Status == JobStatus.Draft ? "draft" : "rejected",
                j.CreatedAt, j.PublishedAt, j.Deadline, j.SectorName, j.Salary
            })
            .ToListAsync(ct);

        var result = items.Select(j => new JobViewItem(
            j.Id, j.Title, j.Company, j.Location,
            j.SourceName, j.ViewCount, j.Status,
            j.CreatedAt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            j.PublishedAt?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            j.Deadline?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            j.SectorName,
            j.Salary)).ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return Results.Ok(new JobViewsData(
            result, totalCount, page, pageSize, totalPages,
            page < totalPages));
    }
}
