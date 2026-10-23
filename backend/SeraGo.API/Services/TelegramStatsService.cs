using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Services;

public record DailySectorStat(Guid SectorId, string SectorName, int JobCount);

public record DailyJobSummary(
    int TotalJobs,
    IReadOnlyList<DailySectorStat> TopSectors,
    IReadOnlyList<Job> SampleJobs,
    DateTimeOffset PeriodStartEat,
    DateTimeOffset PeriodEndEat);

public record UserSectorJobSummary(
    int MatchingJobsCount,
    IReadOnlyList<string> SectorNames,
    IReadOnlyList<Job> SampleJobs,
    DateTimeOffset PeriodStartEat,
    DateTimeOffset PeriodEndEat);

/// <summary>
/// Computes job market statistics, sector hiring leaders, and personalized user sector breakdowns
/// anchored strictly in East Africa Time (EAT / UTC+3) for scheduled Telegram messages.
/// </summary>
public sealed class TelegramStatsService
{
    private static readonly TimeSpan EatOffset = TimeSpan.FromHours(3);
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TelegramStatsService> _logger;

    public TelegramStatsService(ApplicationDbContext db, ILogger<TelegramStatsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public static DateTimeOffset NowEat => DateTimeOffset.UtcNow.ToOffset(EatOffset);

    /// <summary>
    /// Computes full statistics for yesterday (00:00:00 to 23:59:59 EAT). Used for Good Morning broadcasts.
    /// </summary>
    public async Task<DailyJobSummary> GetYesterdayStatsAsync(CancellationToken ct = default)
    {
        var nowEat = NowEat;
        var todayStartEat = new DateTimeOffset(nowEat.Year, nowEat.Month, nowEat.Day, 0, 0, 0, EatOffset);
        var yesterdayStartEat = todayStartEat.AddDays(-1);
        var yesterdayEndEat = todayStartEat;

        var startUtc = yesterdayStartEat.ToUniversalTime();
        var endUtc = yesterdayEndEat.ToUniversalTime();

        return await QuerySummaryAsync(startUtc, endUtc, yesterdayStartEat, yesterdayEndEat, ct);
    }

    /// <summary>
    /// Computes statistics for jobs posted this morning (from 00:00:00 EAT up to 12:00:00 EAT or current time).
    /// Used for Good Afternoon midday pulse messages.
    /// </summary>
    public async Task<DailyJobSummary> GetMorningStatsAsync(CancellationToken ct = default)
    {
        var nowEat = NowEat;
        var todayStartEat = new DateTimeOffset(nowEat.Year, nowEat.Month, nowEat.Day, 0, 0, 0, EatOffset);
        var morningCutoffEat = todayStartEat.AddHours(12);
        var effectiveEndEat = nowEat < morningCutoffEat ? nowEat : morningCutoffEat;

        var startUtc = todayStartEat.ToUniversalTime();
        var endUtc = effectiveEndEat.ToUniversalTime();

        return await QuerySummaryAsync(startUtc, endUtc, todayStartEat, effectiveEndEat, ct);
    }

    /// <summary>
    /// Computes full statistics for today (from 00:00:00 EAT up to the current moment).
    /// Used for Good Night daily wrap-up messages.
    /// </summary>
    public async Task<DailyJobSummary> GetTodayFullStatsAsync(CancellationToken ct = default)
    {
        var nowEat = NowEat;
        var todayStartEat = new DateTimeOffset(nowEat.Year, nowEat.Month, nowEat.Day, 0, 0, 0, EatOffset);

        var startUtc = todayStartEat.ToUniversalTime();
        var endUtc = nowEat.ToUniversalTime();

        return await QuerySummaryAsync(startUtc, endUtc, todayStartEat, nowEat, ct);
    }

    /// <summary>
    /// Computes personalized stats for a specific user's preferred sectors within a time window.
    /// </summary>
    public async Task<UserSectorJobSummary> GetUserSectorStatsAsync(
        IReadOnlyCollection<Guid> sectorIds,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct = default)
    {
        var periodStartEat = startUtc.ToOffset(EatOffset);
        var periodEndEat = endUtc.ToOffset(EatOffset);

        // Fetch sector names
        var sectorNames = new List<string>();
        if (sectorIds.Count > 0)
        {
            sectorNames = await _db.Sectors.AsNoTracking()
                .Where(s => sectorIds.Contains(s.Id))
                .Select(s => s.Name)
                .ToListAsync(ct);
        }

        var query = _db.Jobs.AsNoTracking()
            .Include(j => j.Sector)
            .Where(j => j.Status == JobStatus.Published &&
                        j.IsActive &&
                        (j.PublishedAt ?? j.CreatedAt) >= startUtc &&
                        (j.PublishedAt ?? j.CreatedAt) < endUtc);

        if (sectorIds.Count > 0)
        {
            query = query.Where(j => j.SectorId != null && sectorIds.Contains(j.SectorId.Value));
        }

        var totalCount = await query.CountAsync(ct);

        var sampleJobs = await query
            .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        return new UserSectorJobSummary(
            MatchingJobsCount: totalCount,
            SectorNames: sectorNames,
            SampleJobs: sampleJobs,
            PeriodStartEat: periodStartEat,
            PeriodEndEat: periodEndEat);
    }

    private async Task<DailyJobSummary> QuerySummaryAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        DateTimeOffset startEat,
        DateTimeOffset endEat,
        CancellationToken ct)
    {
        var baseQuery = _db.Jobs.AsNoTracking()
            .Where(j => j.Status == JobStatus.Published &&
                        j.IsActive &&
                        (j.PublishedAt ?? j.CreatedAt) >= startUtc &&
                        (j.PublishedAt ?? j.CreatedAt) < endUtc);

        var totalJobs = await baseQuery.CountAsync(ct);

        var sectorCounts = await baseQuery
            .Where(j => j.SectorId != null)
            .GroupBy(j => j.SectorId!.Value)
            .Select(g => new { SectorId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(ct);

        var sectorIds = sectorCounts.Select(x => x.SectorId).ToList();
        var sectorNames = sectorIds.Count > 0
            ? await _db.Sectors.AsNoTracking()
                .Where(s => sectorIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct)
            : new Dictionary<Guid, string>();

        var sectorStatsRaw = sectorCounts
            .Select(x => new DailySectorStat(
                x.SectorId,
                sectorNames.TryGetValue(x.SectorId, out var name) ? name : "General",
                x.Count))
            .ToList();

        var sampleJobs = await baseQuery
            .Include(j => j.Sector)
            .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
            .Take(4)
            .ToListAsync(ct);

        return new DailyJobSummary(
            TotalJobs: totalJobs,
            TopSectors: sectorStatsRaw,
            SampleJobs: sampleJobs,
            PeriodStartEat: startEat,
            PeriodEndEat: endEat);
    }
}
