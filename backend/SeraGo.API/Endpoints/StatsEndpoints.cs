using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using SeraGo.API.Services;
using SeraGo.Core.Domain;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Admin dashboard stats for the scraper — top sectors and top websites per
/// period, read from the scraper's PERSISTENT stat tables (ScrapeStat +
/// CategoryStat, which survive the weekly archive that prunes the raw logs
/// and job rows).
///
///   GET /api/admin/stats/top?period=day|week|month|year&limit=10 (admin)
///
/// Boundaries use Addis Ababa time (UTC+3, no DST) so they match the day
/// buckets the scraper records under. Top websites come from the matching
/// ScrapeStat row's per-source breakdown (sorted by items found); top
/// sectors come from the day-granular CategoryStat rows summed over the
/// period — the same values the scraper's weekly/monthly report shows, but
/// queryable at any time.
/// </summary>
public static class StatsEndpoints
{
    public static IEndpointRouteBuilder MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin/stats").WithTags("Stats (admin)");
        admin.MapGet("/top", GetTopStatsAsync)
            .RequireRateLimiting("jobs_read")
            .WithOpenApi();
        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record SectorCountResponse(string Name, int Count);

    public sealed record WebsiteStatResponse(
        string Slug, string Name, int ItemsFound, int ItemsInserted,
        int RunCount, int ApiHits);

    public sealed record StatsTopResponse(
        string Period, DateOnly Start, DateOnly End,
        List<SectorCountResponse> TopSectors,
        List<WebsiteStatResponse> TopWebsites);

    // -------------------------------------------------------------- Handlers

    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> GetTopStatsAsync(
        ScraperDbOptions scraperDb,
        string? period = null,
        int limit = 10,
        CancellationToken ct = default)
    {
        if (!scraperDb.IsConfigured)
        {
            return Results.Problem(
                "Scraper database is not configured (set DB_HOST/DB_NAME/DB_USER/DB_PASSWORD "
                + "in .env(SeraGo-Scraper)).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        period = (period ?? "week").Trim().ToLowerInvariant();
        if (period is not ("day" or "week" or "month" or "year"))
        {
            return Results.Problem(
                $"Unknown period '{period}' — use day, week, month or year.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        limit = Math.Clamp(limit, 1, 100);

        var (start, end) = PeriodBounds(period);

        await using var conn = new NpgsqlConnection(scraperDb.ConnectionString);
        await conn.OpenAsync(ct);

        // slug -> display name for the website rows.
        var sourceNames = new Dictionary<string, string>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT slug, name FROM core_source";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                sourceNames[reader.GetString(0)] = reader.GetString(1);
            }
        }

        // Top websites: the matching ScrapeStat row's per-source breakdown.
        var topWebsites = new List<WebsiteStatResponse>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT period_end, by_source::text
                FROM core_scrapestat
                WHERE period_type = @period AND period_start = @start
                """;
            cmd.Parameters.AddWithValue("period", period);
            cmd.Parameters.AddWithValue("start", start);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                // Trust the stored end (the week/month/year rows are
                // authoritative — e.g. the week row knows its Sunday).
                end = reader.GetFieldValue<DateOnly>(0);
                topWebsites = ParseBySource(reader.GetString(1), sourceNames, limit);
            }
        }

        // Top sectors: sum the persistent day-granular CategoryStat rows.
        var topSectors = new List<SectorCountResponse>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT category_name, SUM(count)::int AS total
                FROM core_categorystat
                WHERE category_type = 'sector'
                  AND period_start BETWEEN @start AND @end
                GROUP BY category_name
                ORDER BY total DESC
                LIMIT @limit
                """;
            cmd.Parameters.AddWithValue("start", start);
            cmd.Parameters.AddWithValue("end", end);
            cmd.Parameters.AddWithValue("limit", limit);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                topSectors.Add(new SectorCountResponse(reader.GetString(0), reader.GetInt32(1)));
            }
        }

        return Results.Ok(new StatsTopResponse(period, start, end, topSectors, topWebsites));
    }

    // --------------------------------------------------------------- Helpers

    /// <summary>(start, end) of the current Addis-Ababa period (UTC+3, no DST).</summary>
    private static (DateOnly Start, DateOnly End) PeriodBounds(string period)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        return period switch
        {
            "day" => (today, today),
            "week" => (StartOfWeek(today), StartOfWeek(today).AddDays(6)),
            "month" => (new DateOnly(today.Year, today.Month, 1),
                new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))),
            _ => (new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31)),
        };
    }

    private static DateOnly StartOfWeek(DateOnly day)
    {
        // .NET DayOfWeek: Monday = 1 ... Sunday = 0 — normalize to a Monday start.
        var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
        return day.AddDays(-daysSinceMonday);
    }

    private static List<WebsiteStatResponse> ParseBySource(
        string json, Dictionary<string, string> sourceNames, int limit)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new List<WebsiteStatResponse>();
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            var slug = property.Name;
            var src = property.Value;
            result.Add(new WebsiteStatResponse(
                slug,
                sourceNames.TryGetValue(slug, out var name) ? name : slug,
                GetInt(src, "items_found"),
                GetInt(src, "items_inserted"),
                GetInt(src, "run_count"),
                GetInt(src, "api_hits")));
        }
        return result
            .OrderByDescending(w => w.ItemsFound)
            .ThenByDescending(w => w.ItemsInserted)
            .Take(limit)
            .ToList();
    }

    private static int GetInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : 0;
}
