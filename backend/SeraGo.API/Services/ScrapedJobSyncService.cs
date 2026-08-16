using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.API.Services;

/// <summary>
/// Imports published jobs from the scraper's database (Neon) into SeraGo's
/// Jobs table. This is the single implementation behind every trigger: the
/// admin "Sync scraped jobs" endpoint, the auto-sync scheduler
/// (<see cref="SyncScheduler"/>), and the scraper-side webhook (later).
/// Re-runs are idempotent — existing (SourceName, ExternalId) pairs are
/// updated in place, never duplicated — and concurrent triggers are
/// serialized by an internal gate, so overlapping runs are safe.
///
/// The first run is a full bootstrap; later runs are INCREMENTAL — only
/// items whose scraper updated_at is newer than the stored cursor (see
/// <see cref="SyncState"/>) are pulled, so the cost scales with what
/// changed, not with how much data exists.
/// </summary>
public sealed class ScrapedJobSyncService
{
    public sealed record SyncResult(
        int Inserted, int Updated, int Unchanged, int Uncategorized,
        List<string> UnknownSectors, int Deactivated);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScrapedJobSyncService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ScrapedJobSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScrapedJobSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Runs one full sync. Returns the result, or null when the scraper
    /// database is not configured (callers decide how to surface that).
    /// </summary>
    public async Task<SyncResult?> RunAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return await RunCoreAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SyncResult?> RunCoreAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        // Resolve the registered singleton directly: IOptions<ScraperDbOptions>
        // would create a fresh EMPTY instance (the type is registered as a
        // plain singleton, not via Options.Create), so IsConfigured would
        // always be false and the sync would always 400.
        var options = scope.ServiceProvider.GetRequiredService<ScraperDbOptions>();

        if (!options.IsConfigured)
        {
            return null;
        }

        // Import as the admin: scraped jobs are platform content, not one user's.
        var admin = (await userManager.GetUsersInRoleAsync(Roles.Admin)).FirstOrDefault();
        if (admin is null)
        {
            throw new InvalidOperationException("No admin account exists to attribute imported jobs to.");
        }

        // Preload the vocabulary once — the sync normalizes hundreds of rows.
        var sectors = await db.Sectors.AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Aliases)
            .ToListAsync(ct);

        // The sync cursor (single row, Id = 1). Null on the very first run —
        // that run is a full bootstrap. Afterwards only items whose scraper
        // updated_at is newer than the cursor are pulled.
        var state = await db.SyncState.FirstOrDefaultAsync(s => s.Id == 1, ct);
        var watermark = state?.LastSyncedAt;
        var isBootstrap = watermark is null;

        var inserted = 0;
        var updated = 0;
        var unchanged = 0;
        var uncategorized = 0;
        var deactivated = 0;
        var unknownSectors = new SortedSet<string>();
        // Bootstrap only: every active listing seen — the set-based deactivation
        // pass at the end hides SeraGo jobs whose key is NOT in this set.
        // Incremental runs deactivate per-item instead (the is_active branch
        // in the loop below), so they never need a full-table scan.
        var seenKeys = new HashSet<string>();
        // Newest scraper updated_at processed — becomes the next cursor.
        DateTime? maxUpdatedAt = null;

        // Load every existing scraped-sourced job ONCE, then match in memory.
        // The previous per-row FirstOrDefaultAsync was an N+1 — one EF query
        // per scraper row (~1,700 queries on the last full sync). The unique
        // (SourceName, ExternalId) index guarantees at most one row per key.
        // Reused by the deactivation pass below, so that pass needs no query
        // of its own either.
        var existingJobs = await db.Jobs
            .Where(j => j.SourceName != null && j.ExternalId != null)
            .ToListAsync(ct);
        var jobsByKey = new Dictionary<(string SourceName, string ExternalId), Job>(existingJobs.Count);
        foreach (var existing in existingJobs)
        {
            jobsByKey[(existing.SourceName!, existing.ExternalId!)] = existing;
        }

        await using var conn = new NpgsqlConnection(options.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = isBootstrap ? SyncQueryBootstrap : SyncQueryIncremental;
        if (!isBootstrap)
        {
            cmd.Parameters.AddWithValue("watermark", watermark!.Value);
        }
        // Lifecycle window (deadline + 7 days — the same rule as the public
        // feed): past-window listings are never imported, so a SeraGo row
        // deleted by the weekly cleanup can't be resurrected by the next sync.
        cmd.Parameters.AddWithValue(
            "lifecycleCutoff",
            DateTime.UtcNow.AddDays(-JobLifecycle.GraceDays));
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var externalId = reader.GetString(reader.GetOrdinal("external_id"));
            var sourceSlug = reader.GetString(reader.GetOrdinal("source_slug"));
            var sourceName = DisplaySourceName(sourceSlug);
            var isActive = reader.GetBoolean(reader.GetOrdinal("is_active"));
            var updatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"));
            if (maxUpdatedAt is null || updatedAt > maxUpdatedAt)
            {
                maxUpdatedAt = updatedAt;
            }

            if (!isActive)
            {
                // The listing disappeared — hide its job. Deactivation happens
                // per-item here (driven by the scraper's is_active flip), so
                // incremental runs never need a full-table scan.
                if (jobsByKey.TryGetValue((sourceName, externalId), out var gone) && gone.IsActive)
                {
                    gone.IsActive = false;
                    gone.UpdatedAt = DateTimeOffset.UtcNow;
                    deactivated++;
                }
                continue; // gone listings are never re-imported
            }

            if (isBootstrap)
            {
                seenKeys.Add($"{sourceName}|{externalId}");
            }

            var rawSector = PickRawSector(reader, sourceSlug, out var extraSector);
            var title = reader.IsDBNull(reader.GetOrdinal("title")) ? string.Empty : reader.GetString(reader.GetOrdinal("title"));
            var sector = ResolveSector(sectors, rawSector, title);
            if (sector is null)
            {
                uncategorized++;
                if (!string.IsNullOrWhiteSpace(rawSector))
                {
                    unknownSectors.Add(rawSector);
                }
            }

            var job = jobsByKey.TryGetValue((sourceName, externalId), out var existing)
                ? existing
                : null;

            var jobType = MapJobType(ReadString(reader, "job_type"));
            var publishedAt = ReadDateTime(reader, "published_at");
            var deadline = ReadDateTime(reader, "deadline");
            var description = ReadString(reader, "description");
            var company = ReadString(reader, "company");
            var location = ReadString(reader, "location");
            var url = ReadString(reader, "url");
            var salary = ReadString(reader, "salary");
            var logo = ReadString(reader, "company_logo") ?? ReadString(reader, "entity_logo");
            // Blank logos are stored as null (matching the insert path) so
            // the equality comparison and the stored value stay consistent.
            logo = string.IsNullOrWhiteSpace(logo) ? null : logo;
            var experience = MapExperience(reader, sourceSlug);

            // Compare EVERY imported field — the old 4-field subset meant
            // edits to description/company/location/url/salary/publishedAt on
            // the source never propagated, so the shelf silently went stale.
            var isSame = job is not null
                && job.Title == title
                && job.Description == (description ?? string.Empty)
                && job.Company == (company ?? string.Empty)
                && job.Location == (location ?? string.Empty)
                && job.JobType == jobType
                && job.Url == (url ?? string.Empty)
                && job.Salary == (salary ?? string.Empty)
                && job.PublishedAt == publishedAt
                && job.Deadline == deadline
                && job.SectorId == sector?.Id
                && job.SectorName == sector?.Name
                && job.CompanyLogoUrl == logo
                && job.ExperienceLevel == experience;
            if (job is not null)
            {
                if (isSame)
                {
                    unchanged++;
                    continue;
                }
                job.Title = title;
                job.Description = description ?? string.Empty;
                job.Company = company ?? string.Empty;
                job.Location = location ?? string.Empty;
                job.JobType = jobType;
                job.Url = url ?? string.Empty;
                job.Salary = salary ?? string.Empty;
                job.PublishedAt = publishedAt;
                job.Deadline = deadline;
                job.SectorId = sector?.Id;
                job.SectorName = sector?.Name;
                job.CompanyLogoUrl = logo;
                job.ExperienceLevel = experience;
                job.Status = JobStatus.Published;
                // IsActive is deliberately NOT forced true here: an admin who
                // hid this job keeps it hidden even though the listing is
                // still live on the source. Only the deactivation pass below
                // (and inserts) touch IsActive.
                job.UpdatedAt = DateTimeOffset.UtcNow;
                updated++;
            }
            else
            {
                var newJob = new Job
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Description = description ?? string.Empty,
                    Company = company ?? string.Empty,
                    Location = location ?? string.Empty,
                    JobType = jobType,
                    Url = url ?? string.Empty,
                    Salary = salary ?? string.Empty,
                    PublishedAt = publishedAt,
                    Deadline = deadline,
                    Status = JobStatus.Published,
                    IsActive = true,
                    PostedByUserId = admin.Id,
                    SubmittedAt = publishedAt,
                    ApprovedAt = publishedAt,
                    SourceName = sourceName,
                    SourceUrl = string.IsNullOrWhiteSpace(url) ? null : url,
                    ExternalId = externalId,
                    CompanyLogoUrl = string.IsNullOrWhiteSpace(logo) ? null : logo,
                    SectorId = sector?.Id,
                    SectorName = sector?.Name,
                    ExperienceLevel = string.IsNullOrWhiteSpace(experience) ? null : experience,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                db.Jobs.Add(newJob);
                // Track it so a duplicate key later in the same result set
                // updates this row instead of inserting a second one.
                jobsByKey[(sourceName, externalId)] = newJob;
                inserted++;
            }

            // Save in batches so one long import never holds a huge change set.
            if ((inserted + updated + unchanged) % 100 == 0)
            {
                await db.SaveChangesAsync(ct);
            }
        }
        await db.SaveChangesAsync(ct);

        // Bootstrap deactivation: hide SeraGo jobs whose listing never appeared
        // in the scraper's active set (e.g. old seed rows). Reuses the
        // existingJobs already loaded above, so no extra query. Incremental
        // runs skip this — deactivation happened per-item in the loop.
        // Admin-hidden jobs (IsActive already false) are never touched.
        if (isBootstrap)
        {
            foreach (var job in existingJobs)
            {
                if (!job.IsActive)
                {
                    continue; // admin-hidden or already hidden
                }
                if (seenKeys.Contains($"{job.SourceName}|{job.ExternalId}"))
                {
                    continue;
                }
                job.IsActive = false;
                job.UpdatedAt = DateTimeOffset.UtcNow;
                deactivated++;
            }
            if (deactivated > 0)
            {
                await db.SaveChangesAsync(ct);
            }
        }

        // Advance the cursor to the NEWEST updated_at processed — not "now".
        // If a run fails partway, the next run re-processes from the old
        // cursor; re-processing is idempotent, so nothing is lost or doubled.
        if (maxUpdatedAt is not null)
        {
            var cursor = DateTime.SpecifyKind(maxUpdatedAt.Value, DateTimeKind.Utc);
            if (state is null)
            {
                db.SyncState.Add(new SyncState { Id = 1, LastSyncedAt = cursor });
            }
            else if (cursor > state.LastSyncedAt)
            {
                state.LastSyncedAt = cursor;
            }
            await db.SaveChangesAsync(ct);
        }

        return new SyncResult(
            inserted, updated, unchanged, uncategorized,
            unknownSectors.ToList(), deactivated);
    }

    // --------------------------------------------------------------- Helpers

    /// <summary>
    /// The source's own sector value, per source. Returns null when the
    /// source carries none. Extra (sub-sector) values are ignored for now —
    /// they become fields when the taxonomy grows its second level.
    /// </summary>
    private static string? PickRawSector(DbDataReader reader, string sourceSlug, out string? extraSector)
    {
        extraSector = null;
        switch (sourceSlug)
        {
            case "afriwork":
                // sectors is a JSON array like ["Sales & Promotion"].
                var json = ReadString(reader, "afriwork_sectors");
                return FirstJsonElement(json);
            case "hahujobs":
                extraSector = ReadString(reader, "hahu_sub_sector");
                return ReadString(reader, "hahu_sector");
            case "ethiojobs":
                // catalogs is an array of { id, name, options } objects.
                var catalogs = ReadString(reader, "ethio_catalogs");
                return FirstCatalogName(catalogs);
            default:
                return null; // GeezJobs / Reporter carry no sector
        }
    }

    private static Sector? ResolveSector(
        List<Sector> sectors, string? rawSector, string? title)
    {
        if (!string.IsNullOrWhiteSpace(rawSector))
        {
            var normalized = SectorSeedData.Normalize(rawSector);
            var hit = sectors.FirstOrDefault(s => s.Aliases.Any(a => a.Alias == normalized));
            if (hit is not null)
            {
                return hit;
            }
        }
        var slug = SectorNormalizer.ClassifyTitle(title);
        return slug is null ? null : sectors.FirstOrDefault(s => s.Slug == slug);
    }

    /// <summary>Extracts the first string element of a JSON array (e.g. Afriwork's sectors).</summary>
    private static string? FirstJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array
                && doc.RootElement.GetArrayLength() > 0
                && doc.RootElement[0].ValueKind == JsonValueKind.String)
            {
                return doc.RootElement[0].GetString();
            }
        }
        catch (JsonException)
        {
            // Not JSON — treat the raw text as the sector name.
            return json;
        }
        return null;
    }

    /// <summary>Extracts the first object's "name" from EthioJobs' catalogs array.</summary>
    private static string? FirstCatalogName(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object
                        && item.TryGetProperty("name", out var name)
                        && name.ValueKind == JsonValueKind.String)
                    {
                        return name.GetString();
                    }
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }
        return null;
    }

    /// <summary>Source slug → display name (matches the existing seed/branding).</summary>
    private static string DisplaySourceName(string slug) => slug switch
    {
        "afriwork" => "Afriwork",
        "ethiojobs" => "EthioJobs",
        "geezjobs" => "GeezJobs",
        "hahujobs" => "HaHuJobs",
        "reporterjobs" => "Ethiopian Reporter Jobs",
        _ => slug,
    };

    /// <summary>Tolerant job-type mapping: ints, common strings, else Other.</summary>
    private static JobType MapJobType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return JobType.Other;
        }
        if (int.TryParse(value, out var n) && Enum.IsDefined(typeof(JobType), n))
        {
            return (JobType)n;
        }
        var text = value.ToLowerInvariant();
        if (text.Contains("part")) return JobType.PartTime;
        if (text.Contains("contract")) return JobType.Contract;
        if (text.Contains("remote")) return JobType.Remote;
        if (text.Contains("intern")) return JobType.Internship;
        if (text.Contains("freelance")) return JobType.Freelance;
        if (text.Contains("temporary")) return JobType.Temporary;
        if (text.Contains("full")) return JobType.FullTime;
        return JobType.Other;
    }

    /// <summary>Experience band from the source's year hints, else null.</summary>
    private static string? MapExperience(DbDataReader reader, string sourceSlug)
    {
        double? years = null;
        switch (sourceSlug)
        {
            case "geezjobs":
                years = ReadDouble(reader, "geez_min_exp");
                break;
            case "hahujobs":
                // years_of_experience is an INTEGER column in Postgres, and
                // Npgsql rejects GetString on it (SQLite was lenient) — read
                // the raw value and convert instead.
                var raw = reader.GetValue(reader.GetOrdinal("hahu_exp"));
                if (raw is not DBNull
                    && double.TryParse(
                        Convert.ToString(raw, CultureInfo.InvariantCulture),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var h))
                {
                    years = h;
                }
                break;
        }
        return years switch
        {
            null => null,
            < 1 => "Entry",
            < 3 => "Junior",
            < 5 => "Mid",
            _ => "Senior",
        };
    }

    private static string? ReadString(DbDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        return reader.IsDBNull(i) ? null : reader.GetString(i);
    }

    private static DateTimeOffset? ReadDateTime(DbDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        if (reader.IsDBNull(i))
        {
            return null;
        }
        var value = reader.GetDateTime(i);
        // Django stores UTC in Postgres timestamptz (returned as Kind=Utc).
        // Treat any Unspecified value as UTC too — casting it as local time
        // would shift Addis-ababa jobs by the server's offset (e.g. +3h).
        if (value.Kind == DateTimeKind.Unspecified)
        {
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        return new DateTimeOffset(value);
    }

    private static double? ReadDouble(DbDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        return reader.IsDBNull(i) ? null : Convert.ToDouble(reader.GetValue(i));
    }

    /// <summary>First-ever run: every ACTIVE listing (full import + set-based deactivation).</summary>
    private const string SyncQueryBootstrap = """
        SELECT
            i.external_id, i.title, i.description, i.company, i.location,
            i.job_type, i.url, i.salary, i.published_at, i.deadline,
            i.is_active, i.updated_at,
            s.name AS source_name, s.slug AS source_slug,
            a.sectors::text AS afriwork_sectors,
            h.sector_name AS hahu_sector, h.sub_sector_name AS hahu_sub_sector,
            h.entity_logo AS entity_logo, h.years_of_experience AS hahu_exp,
            e.catalogs::text AS ethio_catalogs,
            g.employment_text AS geez_employment, g.company_logo AS company_logo,
            g.min_experience_years AS geez_min_exp,
            r.job_type AS reporter_type
        FROM core_scrapeditem i
        JOIN core_source s ON s.id = i.source_id
        LEFT JOIN core_afriworkjob a ON a.id = i.afriwork_job_id
        LEFT JOIN core_hahujob h ON h.id = i.hahujobs_job_id
        LEFT JOIN core_ethiojobsjob e ON e.id = i.ethiojobs_job_id
        LEFT JOIN core_geezjob g ON g.id = i.geezjobs_job_id
        LEFT JOIN core_reporterjob r ON r.id = i.reporter_job_id
        WHERE i.is_active = true
          AND (i.deadline IS NULL OR i.deadline > @lifecycleCutoff)
        """;

    /// <summary>
    /// Later runs: only items CHANGED since the cursor (Django bumps
    /// updated_at on every insert/update, including is_active flips), so the
    /// sync costs what changed, not the whole table.
    /// </summary>
    private const string SyncQueryIncremental = """
        SELECT
            i.external_id, i.title, i.description, i.company, i.location,
            i.job_type, i.url, i.salary, i.published_at, i.deadline,
            i.is_active, i.updated_at,
            s.name AS source_name, s.slug AS source_slug,
            a.sectors::text AS afriwork_sectors,
            h.sector_name AS hahu_sector, h.sub_sector_name AS hahu_sub_sector,
            h.entity_logo AS entity_logo, h.years_of_experience AS hahu_exp,
            e.catalogs::text AS ethio_catalogs,
            g.employment_text AS geez_employment, g.company_logo AS company_logo,
            g.min_experience_years AS geez_min_exp,
            r.job_type AS reporter_type
        FROM core_scrapeditem i
        JOIN core_source s ON s.id = i.source_id
        LEFT JOIN core_afriworkjob a ON a.id = i.afriwork_job_id
        LEFT JOIN core_hahujob h ON h.id = i.hahujobs_job_id
        LEFT JOIN core_ethiojobsjob e ON e.id = i.ethiojobs_job_id
        LEFT JOIN core_geezjob g ON g.id = i.geezjobs_job_id
        LEFT JOIN core_reporterjob r ON r.id = i.reporter_job_id
        WHERE i.updated_at > @watermark
          AND (i.deadline IS NULL OR i.deadline > @lifecycleCutoff)
        """;
}
