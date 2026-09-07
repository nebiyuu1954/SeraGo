using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SeraGo.API.Services;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Admin scraper analytics endpoints:
///   GET /api/admin/scraper/week-stats — list of scraper weeks (period_start,
///     period_end, days_with_runs, run_count, items_inserted, status summary).
///   GET /api/admin/scraper/week/{periodStart} — full detail for one week:
///     every day in the week (master + per-site logs), runs, per-source
///     breakdown, top errors, in clear tabular form.
/// </summary>
public static class AdminScraperEndpoints
{
    public static IEndpointRouteBuilder MapAdminScraperEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/scraper/week-stats", GetWeekStatsListAsync)
            .RequireAuthorization(p => p.RequireRole("Admin"))
            .RequireRateLimiting("jobs_read")
            .WithTags("Scraper (admin)")
            .WithOpenApi();

        app.MapGet("/api/admin/scraper/week/{periodStart}", GetWeekDetailAsync)
            .RequireAuthorization(p => p.RequireRole("Admin"))
            .RequireRateLimiting("jobs_read")
            .WithTags("Scraper (admin)")
            .WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record WeekSummary(
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        int DaysWithRuns,
        int RunCount,
        int ApiHits,
        int ItemsFound,
        int ItemsInserted,
        int ItemsUpdated,
        int ItemsSkipped,
        string StatusSummary,   // e.g. "success: 47, partial: 2, failed: 1"
        string? WorstDay        // day label with the worst status, e.g. "2026-08-08 (failed)"
    );

    public sealed record WeekStatsListResponse(
        List<WeekSummary> Weeks,
        int Total
    );

    // One day in the week — master log row.
    public sealed record ScraperDay(
        DateOnly Day,
        string Status,
        int RunCount,
        int ApiHits,
        int ItemsFound,
        int ItemsInserted,
        int ItemsUpdated,
        int ItemsSkipped,
        int WebsitesCount,
        List<ScraperDayWebsite> Websites,
        List<ScraperRunSummary> Runs
    );

    public sealed record ScraperDayWebsite(
        string Source,
        string Name,
        string Table,
        Guid LogId,
        string Status,
        int RunCount,
        int ApiHits,
        int ItemsFound,
        int ItemsInserted,
        int ItemsUpdated,
        int ItemsSkipped
    );

    public sealed record ScraperRunSummary(
        int Run,
        string Time,
        int Hits,
        int Found,
        int Inserted,
        int Updated,
        int Skipped,
        string Status,
        string? Message,
        string? Errors
    );

    // One per-site log for the week (from ScrapeStat.by_source).
    public sealed record ScraperWeekSource(
        string Source,
        string Name,
        int DaysActive,
        int RunCount,
        int ApiHits,
        int ItemsFound,
        int ItemsInserted,
        int ItemsUpdated,
        int ItemsSkipped,
        string StatusSummary
    );

    public sealed record WeekDetailResponse(
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        string PeriodLabel,     // e.g. "Week of Aug 3, 2026"
        int DaysWithRuns,
        int TotalRunCount,
        int TotalApiHits,
        int TotalItemsFound,
        int TotalItemsInserted,
        int TotalItemsUpdated,
        int TotalItemsSkipped,
        string StatusSummary,
        List<ScraperDay> Days,
        List<ScraperWeekSource> Sources,
        List<ScraperErrorSummary> TopErrors,
        string? ArchiveNote     // e.g. "Archived to Telegram on 2026-08-17"
    );

    public sealed record ScraperErrorSummary(
        string Message,
        int Count
    );

    // ------------------------------------------------------------- Handlers

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetWeekStatsListAsync(
        ScraperDbOptions scraperDb,
        CancellationToken ct)
    {
        if (!scraperDb.IsConfigured)
        {
            return Results.Problem(
                "Scraper database is not configured (set DB_HOST/DB_NAME/DB_USER/DB_PASSWORD "
                + "in .env(SeraGo-Scraper)).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await using var conn = new NpgsqlConnection(scraperDb.ConnectionString);
        await conn.OpenAsync(ct);

        // All week-period ScrapeStat rows, newest first.
        var weeks = new List<WeekSummary>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT period_start, period_end, days_with_runs, run_count,
                       api_hits, items_found, items_inserted, items_updated,
                       items_skipped, runs_by_status::text
                FROM core_scrapestat
                WHERE period_type = 'week'
                ORDER BY period_start DESC
                """;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var start = reader.GetFieldValue<DateOnly>(0);
                var end = reader.GetFieldValue<DateOnly>(1);
                var daysWithRuns = reader.GetFieldValue<int>(2);
                var runCount = reader.GetFieldValue<int>(3);
                var apiHits = reader.GetFieldValue<int>(4);
                var itemsFound = reader.GetFieldValue<int>(5);
                var itemsInserted = reader.GetFieldValue<int>(6);
                var itemsUpdated = reader.GetFieldValue<int>(7);
                var itemsSkipped = reader.GetFieldValue<int>(8);
                var runsByStatusJson = reader.GetString(9);

                weeks.Add(new WeekSummary(
                    start, end, daysWithRuns, runCount, apiHits,
                    itemsFound, itemsInserted, itemsUpdated, itemsSkipped,
                    StatusSummaryTextFromJson(runsByStatusJson),
                    WorstDayLabelFromWeek(conn, start)
                ));
            }
        }

        return Results.Ok(new WeekStatsListResponse(weeks, weeks.Count));
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetWeekDetailAsync(
        string periodStart,
        ScraperDbOptions scraperDb,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        if (!DateOnly.TryParse(periodStart, out var parsedDate))
        {
            return Results.BadRequest(new { message = "Invalid date format. Use YYYY-MM-DD." });
        }

        if (!scraperDb.IsConfigured)
        {
            return Results.Problem(
                "Scraper database is not configured.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            await using var conn = new NpgsqlConnection(scraperDb.ConnectionString);
            await conn.OpenAsync(ct);

            // The persistent ScrapeStat row for this week.
            ScrapeStatRow? stat = null;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT period_start, period_end, days_with_runs, run_count,
                           api_hits, items_found, items_inserted, items_updated,
                           items_skipped, runs_by_status::text,
                           top_errors::text, by_source::text
                    FROM core_scrapestat
                    WHERE period_type = 'week' AND period_start = @start
                    """;
                cmd.Parameters.AddWithValue("start", parsedDate);
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    stat = new ScrapeStatRow(
                        reader.GetFieldValue<DateOnly>(0),
                        reader.GetFieldValue<DateOnly>(1),
                        reader.GetFieldValue<int>(2),
                        reader.GetFieldValue<int>(3),
                        reader.GetFieldValue<int>(4),
                        reader.GetFieldValue<int>(5),
                        reader.GetFieldValue<int>(6),
                        reader.GetFieldValue<int>(7),
                        reader.GetFieldValue<int>(8),
                        reader.GetString(9),
                        reader.IsDBNull(10) ? null : reader.GetString(10),
                        reader.IsDBNull(11) ? null : reader.GetString(11)
                    );
                }
            }

            if (stat is null)
            {
                return Results.NotFound(new { message = "No scraper data found for the week of " + parsedDate.ToString("MMM d yyyy") + "." });
            }

            // Day-level master logs for the days in this week.
            var dayStart = parsedDate;
            var dayEnd = parsedDate.AddDays(6);
            var days = new List<ScraperDayRow>();
            await using (var cmd = conn.CreateCommand())
            {
            cmd.CommandText = """
                SELECT day, status, run_count, api_hits,
                       items_found, items_inserted, items_updated, items_skipped,
                       websites::text, runs::text
                FROM core_scrapelog
                WHERE day >= @start AND day <= @end
                ORDER BY day DESC
                """;
            cmd.Parameters.AddWithValue("start", dayStart);
            cmd.Parameters.AddWithValue("end", dayEnd);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var websitesJson = reader.IsDBNull(8) ? null : reader.GetString(8);
                var websitesCount = websitesJson is null ? 0 : CountJsonArrayItems(websitesJson);
                days.Add(new ScraperDayRow(
                    reader.GetFieldValue<DateOnly>(0),
                    reader.GetString(1),
                    reader.GetFieldValue<int>(2),
                    reader.GetFieldValue<int>(3),
                    reader.GetFieldValue<int>(4),
                    reader.GetFieldValue<int>(5),
                    reader.GetFieldValue<int>(6),
                    reader.GetFieldValue<int>(7),
                    websitesJson,
                    reader.IsDBNull(9) ? null : reader.GetString(9),
                    websitesCount
                ));
            }
        }

        var dayDetails = days.Select(d => new ScraperDay(
            d.Day, d.Status, d.RunCount, d.ApiHits,
            d.ItemsFound, d.ItemsInserted, d.ItemsUpdated, d.ItemsSkipped,
            d.WebsitesCount, ParseWebsitesJson(d.Websites), ParseRunsJson(d.Runs)
        )).ToList();

            // Per-source breakdown from the persistent stat.
            var sources = ParseBySourceJson(stat.BySource);

            // Top errors.
            var topErrors = ParseTopErrorsJson(stat.TopErrors);

            var archiveNote = "Week of " + parsedDate.ToString("MMM d")
                + " \u2013 " + parsedDate.AddDays(6).ToString("MMM d, yyyy")
                + (stat.ItemsInserted > 0
                    ? " \u2014 " + stat.ItemsInserted.ToString("N0") + " jobs inserted, " + stat.RunCount.ToString("N0") + " runs"
                    : "");

            var periodLabel = "Week of " + parsedDate.ToString("MMMM d, yyyy");

            return Results.Ok(new WeekDetailResponse(
                parsedDate,
                stat.PeriodEnd,
                periodLabel,
                stat.DaysWithRuns,
                stat.RunCount,
                stat.ApiHits,
                stat.ItemsFound,
                stat.ItemsInserted,
                stat.ItemsUpdated,
                stat.ItemsSkipped,
                StatusSummaryTextFromJson(stat.RunsByStatus),
                dayDetails,
                sources,
                topErrors,
                archiveNote
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem(
                "Failed to load scraper week data: " + ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    // ------------------------------------------------------------- JSON helpers

    private static string StatusSummaryTextFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "—";
        try
        {
            using var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, int>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Number)
                    dict[prop.Name] = prop.Value.GetInt32();
            }
            if (dict.Count == 0)
                return "—";
            return string.Join(", ",
                dict.OrderByDescending(kv => kv.Value)
                    .Select(kv => $"{kv.Value} {kv.Key}"));
        }
        catch
        {
            return "—";
        }
    }

    private static string? WorstDayLabelFromWeek(NpgsqlConnection conn, DateOnly weekStart)
    {
        // Look at the day logs in this week and find the one with the worst status.
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT day, status FROM core_scrapelog
                WHERE day >= @start AND day <= @end
                ORDER BY
                    CASE status WHEN 'failed' THEN 1 WHEN 'partial' THEN 2 ELSE 3 END,
                    day DESC
                LIMIT 1
                """;
            cmd.Parameters.AddWithValue("start", weekStart);
            cmd.Parameters.AddWithValue("end", weekStart.AddDays(6));
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var day = reader.GetFieldValue<DateOnly>(0);
                var status = reader.GetString(1);
                return $"{day:MMM d} ({status})";
            }
        }
        catch { }
        return null;
    }

    private static List<ScraperDayWebsite> ParseWebsitesJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<ScraperDayWebsite>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                list.Add(new ScraperDayWebsite(
                    GetString(el, "source"),
                    GetString(el, "name"),
                    GetString(el, "table"),
                    Guid.TryParse(GetString(el, "log_id"), out var gid) ? gid : Guid.Empty,
                    GetString(el, "status"),
                    GetInt(el, "run_count"),
                    GetInt(el, "api_hits"),
                    GetInt(el, "items_found"),
                    GetInt(el, "items_inserted"),
                    GetInt(el, "items_updated"),
                    GetInt(el, "items_skipped")
                ));
            }
            return list;
        }
        catch { return []; }
    }

    private static List<ScraperRunSummary> ParseRunsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<ScraperRunSummary>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                list.Add(new ScraperRunSummary(
                    GetInt(el, "run"),
                    GetString(el, "time"),
                    GetInt(el, "hits"),
                    GetInt(el, "found"),
                    GetInt(el, "inserted"),
                    GetInt(el, "updated"),
                    GetInt(el, "skipped"),
                    GetString(el, "status"),
                    GetString(el, "message"),
                    GetString(el, "errors")
                ));
            }
            return list;
        }
        catch { return []; }
    }

    private static List<ScraperWeekSource> ParseBySourceJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return [];

            var list = new List<ScraperWeekSource>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var src = prop.Value;
                if (src.ValueKind != JsonValueKind.Object) continue;
                var runsByStatus = src.TryGetProperty("runs_by_status", out var rbs)
                    ? rbs.GetRawText() : null;
                list.Add(new ScraperWeekSource(
                    prop.Name, prop.Name,
                    GetInt(src, "days_active"),
                    GetInt(src, "run_count"),
                    GetInt(src, "api_hits"),
                    GetInt(src, "items_found"),
                    GetInt(src, "items_inserted"),
                    GetInt(src, "items_updated"),
                    GetInt(src, "items_skipped"),
                    StatusSummaryTextFromJson(runsByStatus)
                ));
            }
            return list.OrderByDescending(s => s.RunCount).ToList();
        }
        catch { return []; }
    }

    private static List<ScraperErrorSummary> ParseTopErrorsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];
            var list = new List<ScraperErrorSummary>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                list.Add(new ScraperErrorSummary(
                    el.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String
                        ? msg.GetString()! : "",
                    GetInt(el, "count")));
            }
            return list.OrderByDescending(e => e.Count).ToList();
        }
        catch { return []; }
    }

    private static int CountJsonArrayItems(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.GetArrayLength() : 0;
        }
        catch { return 0; }
    }

    private static int GetInt(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static string GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    // Rows read from the scraper DB.
    private sealed record ScrapeStatRow(
        DateOnly PeriodStart, DateOnly PeriodEnd, int DaysWithRuns,
        int RunCount, int ApiHits, int ItemsFound, int ItemsInserted,
        int ItemsUpdated, int ItemsSkipped, string RunsByStatus,
        string? TopErrors, string? BySource);

    private sealed record ScraperDayRow(
        DateOnly Day, string Status, int RunCount, int ApiHits,
        int ItemsFound, int ItemsInserted, int ItemsUpdated, int ItemsSkipped,
        string? Websites, string? Runs, int WebsitesCount);
}
