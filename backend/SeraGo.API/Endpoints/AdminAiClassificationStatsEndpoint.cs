using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using SeraGo.API.Services;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Admin-only AI classification usage stats — one endpoint, aggregate numbers only.
///
///   GET /api/admin/ai/classification/stats?days=30   (admin JWT)
///
/// Reads ONLY counts and token totals from the SeraGo-AI service database's
/// classify audit tables (<c>ai_service_aiclassificationlog</c> joined to
/// <c>ai_service_aiclassificationraw</c>). No prompts, model responses,
/// reasoning text or job payloads are ever returned.
///
/// Response shape:
///   summary  — all-time cumulative jobs + tokens
///   windows  — today / thisWeek / thisMonth / allTime rollups
///   daily    — per-UTC-day breakdown for the last <c>days</c> days
///              (default 30, max 90)
///
/// Windows are calendar-based in UTC: today = UTC midnight, thisWeek = Monday
/// 00:00 UTC, thisMonth = the 1st 00:00 UTC.
///
///   GET /api/admin/ai/classification/jobs?page=1&pageSize=25   (admin JWT)
///
/// The per-job audit table: one row per classify attempt, with the sector
/// before → after, the model's confidence + reasoning, and the tokens that
/// call used. Reasonings come from the AI service DB; the human-readable job
/// title/company come from this backend's Jobs table (the two live in separate
/// databases, so they are stitched together in memory).
///
/// Cached in-memory for a few minutes so a polling dashboard doesn't hammer the
/// serverless database on every poll.
/// </summary>
public static class AdminAiClassificationStatsEndpoint
{
    private const int DefaultDays = 30;
    private const int MaxDays = 90;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(3);

    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapAdminAiClassificationStatsEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/ai/classification")
            .RequireAuthorization(p => p.RequireRole("Admin"))
            .RequireRateLimiting("jobs_read")
            .WithTags("AI classification (admin)");

        group.MapGet("/stats", GetStatsAsync).WithOpenApi();
        group.MapGet("/jobs", GetJobsAsync).WithOpenApi();
        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record WindowStats(
        int JobsProcessed,
        int JobsSuccessful,
        int JobsUncategorized,
        long TokensSent,
        long TokensReceived,
        long TotalTokens,
        double LatencyMsAvg)
    {
        internal static readonly WindowStats Empty = new(0, 0, 0, 0, 0, 0, 0);
    }

    public sealed record AiClassificationSummary(
        int TotalJobsEver,
        int TotalSuccessfulEver,
        int TotalUncategorizedEver,
        long TotalTokensSentEver,
        long TotalTokensReceivedEver,
        long TotalTokensEver);

    public sealed record AiClassificationWindows(
        WindowStats Today,
        WindowStats ThisWeek,
        WindowStats ThisMonth,
        WindowStats AllTime);

    public sealed record AiClassificationDay(
        string Date,
        int JobsProcessed,
        int JobsSuccessful,
        int JobsUncategorized,
        long TokensSent,
        long TokensReceived,
        long TotalTokens);

    public sealed record AiClassificationJobItem(
        long LogId,
        string JobId,
        string? JobTitle,
        string? JobCompany,
        string? SectorSlug,
        string? SectorName,
        string? OriginalSectorSlug,
        string? OriginalSectorName,
        double? Confidence,
        string? Reasoning,
        bool Categorized,
        string AiClassifiedAt,
        long TokensSent,
        long TokensReceived,
        long TotalTokens,
        int? LatencyMs);

    public sealed record AiClassificationJobsResponse(
        int Page,
        int PageSize,
        int Total,
        int TotalPages,
        List<AiClassificationJobItem> Items,
        string? Message = null);

    public sealed record AiClassificationStatsResponse(
        string GeneratedAt,
        int Days,
        AiClassificationWindows Windows,
        AiClassificationSummary Summary,
        List<AiClassificationDay> Daily,
        string? Message = null)
    {
        internal static AiClassificationStatsResponse Unavailable(int days, string message) =>
            new(
                DateTimeOffset.UtcNow.ToString("o"),
                days,
                new AiClassificationWindows(WindowStats.Empty, WindowStats.Empty, WindowStats.Empty, WindowStats.Empty),
                new AiClassificationSummary(0, 0, 0, 0, 0, 0),
                [],
                message);
    }

    // -------------------------------------------------------------- Handlers

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetStatsAsync(
        AiClassificationDbOptions aiDb,
        IMemoryCache cache,
        ILogger<object> logger,
        int? days,
        CancellationToken ct)
    {
        var window = Math.Clamp(days ?? DefaultDays, 1, MaxDays);
        var cacheKey = $"ai-classify-stats:{window}";

        if (cache.TryGetValue(cacheKey, out AiClassificationStatsResponse? cached) && cached is not null)
        {
            return Results.Ok(cached);
        }

        if (!aiDb.IsConfigured)
        {
            return Results.Ok(AiClassificationStatsResponse.Unavailable(
                window,
                "SeraGo-AI database is not configured — set AI_DB_CONNECTION, or "
                + "AI_DB_HOST / AI_DB_NAME / AI_DB_USER / AI_DB_PASSWORD in .env(SeraGo-AI)."));
        }

        try
        {
            var response = await QueryAsync(aiDb, window, ct);
            cache.Set(cacheKey, response, CacheTtl);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read AI classification stats from the SeraGo-AI database");
            return Results.Ok(AiClassificationStatsResponse.Unavailable(
                window, "SeraGo-AI database is unreachable."));
        }
    }

    /// <summary>
    /// Paged per-job classify audit trail. The AI service DB holds the log
    /// (sector before/after, confidence, reasoning, tokens); this backend's
    /// Jobs table supplies the display title + company.
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetJobsAsync(
        AiClassificationDbOptions aiDb,
        ApplicationDbContext db,
        ILogger<object> logger,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

        if (!aiDb.IsConfigured)
        {
            return Results.Ok(new AiClassificationJobsResponse(
                currentPage, size, 0, 0, [],
                "SeraGo-AI database is not configured — set AI_DB_CONNECTION, or "
                + "AI_DB_HOST / AI_DB_NAME / AI_DB_USER / AI_DB_PASSWORD in .env(SeraGo-AI)."));
        }

        try
        {
            var (total, rows) = await QueryJobLogAsync(aiDb, currentPage, size, ct);

            // Stitch in the job titles from this backend's own database — the
            // classify log only stores the job id.
            var ids = rows
                .Select(r => Guid.TryParse(r.JobId, out var g) ? g : (Guid?)null)
                .Where(g => g is not null)
                .Select(g => g!.Value)
                .Distinct()
                .ToArray();

            var jobInfo = new Dictionary<Guid, (string? Title, string? Company)>();
            if (ids.Length > 0)
            {
                var found = await db.Jobs.AsNoTracking()
                    .Where(j => ids.Contains(j.Id))
                    .Select(j => new { j.Id, j.Title, j.Company })
                    .ToListAsync(ct);
                foreach (var j in found)
                {
                    jobInfo[j.Id] = (j.Title, j.Company);
                }
            }

            var items = rows.Select(r =>
            {
                string? title = null;
                string? company = null;
                if (Guid.TryParse(r.JobId, out var gid) && jobInfo.TryGetValue(gid, out var info))
                {
                    title = info.Title;
                    company = info.Company;
                }

                return new AiClassificationJobItem(
                    r.LogId,
                    r.JobId,
                    title,
                    company,
                    r.SectorSlug,
                    r.SectorName,
                    r.OriginalSectorSlug,
                    r.OriginalSectorName,
                    r.Confidence,
                    r.Reasoning,
                    r.Categorized,
                    r.AiClassifiedAt,
                    r.TokensSent,
                    r.TokensReceived,
                    r.TotalTokens,
                    r.LatencyMs);
            }).ToList();

            var totalPages = size > 0 ? (int)Math.Ceiling(total / (double)size) : 0;
            return Results.Ok(new AiClassificationJobsResponse(currentPage, size, total, totalPages, items));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read AI classification job log from the SeraGo-AI database");
            return Results.Ok(new AiClassificationJobsResponse(
                currentPage, size, 0, 0, [], "SeraGo-AI database is unreachable."));
        }
    }

    /// <summary>Raw row from the classify log (before the job title is stitched in).</summary>
    private sealed record JobLogRow(
        long LogId,
        string JobId,
        string? SectorSlug,
        string? SectorName,
        string? OriginalSectorSlug,
        string? OriginalSectorName,
        double? Confidence,
        string? Reasoning,
        bool Categorized,
        string AiClassifiedAt,
        long TokensSent,
        long TokensReceived,
        long TotalTokens,
        int? LatencyMs);

    private static async Task<(int Total, List<JobLogRow> Rows)> QueryJobLogAsync(
        AiClassificationDbOptions aiDb, int page, int pageSize, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(aiDb.ConnectionString);
        await conn.OpenAsync(ct);

        int total;
        await using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*)::bigint FROM ai_service_aiclassificationlog";
            var scalar = await countCmd.ExecuteScalarAsync(ct);
            total = scalar is null or DBNull ? 0 : (int)Convert.ToInt64(scalar);
        }

        const string sql = """
            SELECT
                l.id,
                l.job_id,
                l.sector_slug,
                l.sector_name,
                l.original_sector_slug,
                l.original_sector_name,
                l.confidence,
                l.reasoning,
                l.categorized,
                l.ai_classified_at,
                COALESCE(r.tokens_sent, 0)::bigint,
                COALESCE(r.tokens_received, 0)::bigint,
                COALESCE(r.total_tokens, 0)::bigint,
                r.latency_ms
            FROM ai_service_aiclassificationlog l
            LEFT JOIN ai_service_aiclassificationraw r ON r.id = l.log_id_id
            ORDER BY l.ai_classified_at DESC
            LIMIT @limit OFFSET @offset
            """;

        var rows = new List<JobLogRow>(pageSize);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            // timestamptz comes back as a UTC DateTime — normalise before formatting
            // so the JSON always carries a proper UTC offset.
            var classifiedAt = DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Utc);

            rows.Add(new JobLogRow(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetDouble(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                !reader.IsDBNull(8) && reader.GetBoolean(8),
                new DateTimeOffset(classifiedAt).ToString("o"),
                reader.GetInt64(10),
                reader.GetInt64(11),
                reader.GetInt64(12),
                reader.IsDBNull(13) ? null : reader.GetInt32(13)));
        }

        return (total, rows);
    }

    // ---------------------------------------------------------------- Query

    private static async Task<AiClassificationStatsResponse> QueryAsync(
        AiClassificationDbOptions aiDb, int days, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(aiDb.ConnectionString);
        await conn.OpenAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var startOfToday = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        // Monday 00:00 UTC of the current week.
        var startOfWeek = startOfToday.AddDays(-(((int)now.UtcDateTime.DayOfWeek + 6) % 7));
        var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var allTime = await QueryWindowAsync(conn, DateTimeOffset.UnixEpoch, ct);
        var today = await QueryWindowAsync(conn, startOfToday, ct);
        var thisWeek = await QueryWindowAsync(conn, startOfWeek, ct);
        var thisMonth = await QueryWindowAsync(conn, startOfMonth, ct);
        var daily = await QueryDailyAsync(conn, startOfToday.AddDays(-(days - 1)), ct);

        return new AiClassificationStatsResponse(
            now.ToString("o"),
            days,
            new AiClassificationWindows(today, thisWeek, thisMonth, allTime),
            new AiClassificationSummary(
                allTime.JobsProcessed,
                allTime.JobsSuccessful,
                allTime.JobsUncategorized,
                allTime.TokensSent,
                allTime.TokensReceived,
                allTime.TotalTokens),
            daily);
    }

    private static async Task<WindowStats> QueryWindowAsync(
        NpgsqlConnection conn, DateTimeOffset cutoffUtc, CancellationToken ct)
    {
        const string sql = """
            SELECT
                COUNT(l.id)::bigint                                  AS jobs_processed,
                COUNT(l.id) FILTER (WHERE l.categorized)::bigint      AS jobs_successful,
                COUNT(l.id) FILTER (WHERE NOT l.categorized)::bigint  AS jobs_uncategorized,
                COALESCE(SUM(r.tokens_sent), 0)::bigint               AS tokens_sent,
                COALESCE(SUM(r.tokens_received), 0)::bigint           AS tokens_received,
                COALESCE(SUM(r.total_tokens), 0)::bigint              AS total_tokens,
                COALESCE(AVG(r.latency_ms), 0)::double precision      AS latency_ms_avg
            FROM ai_service_aiclassificationlog l
            LEFT JOIN ai_service_aiclassificationraw r ON r.id = l.log_id_id
            WHERE l.ai_classified_at >= @cutoff
            """;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("cutoff", cutoffUtc);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            return WindowStats.Empty;
        }

        return new WindowStats(
            (int)reader.GetInt64(0),
            (int)reader.GetInt64(1),
            (int)reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5),
            reader.GetDouble(6));
    }

    private static async Task<List<AiClassificationDay>> QueryDailyAsync(
        NpgsqlConnection conn, DateTimeOffset fromUtc, CancellationToken ct)
    {
        const string sql = """
            SELECT
                (l.ai_classified_at AT TIME ZONE 'UTC')::date         AS day,
                COUNT(l.id)::bigint                                   AS jobs_processed,
                COUNT(l.id) FILTER (WHERE l.categorized)::bigint       AS jobs_successful,
                COUNT(l.id) FILTER (WHERE NOT l.categorized)::bigint   AS jobs_uncategorized,
                COALESCE(SUM(r.tokens_sent), 0)::bigint                AS tokens_sent,
                COALESCE(SUM(r.tokens_received), 0)::bigint            AS tokens_received,
                COALESCE(SUM(r.total_tokens), 0)::bigint               AS total_tokens
            FROM ai_service_aiclassificationlog l
            LEFT JOIN ai_service_aiclassificationraw r ON r.id = l.log_id_id
            WHERE l.ai_classified_at >= @from
            GROUP BY 1
            ORDER BY 1
            """;

        var result = new List<AiClassificationDay>();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("from", fromUtc);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            result.Add(new AiClassificationDay(
                reader.GetFieldValue<DateOnly>(0).ToString("yyyy-MM-dd"),
                (int)reader.GetInt64(1),
                (int)reader.GetInt64(2),
                (int)reader.GetInt64(3),
                reader.GetInt64(4),
                reader.GetInt64(5),
                reader.GetInt64(6)));
        }

        return result;
    }
}
