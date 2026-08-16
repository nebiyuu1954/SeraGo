using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Services;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Sectors — the canonical job-sector vocabulary — plus the admin tooling
/// around it: managing sectors/aliases, the uncategorized review data, and
/// the sync that imports scraped jobs from the scraper database.
///
///   GET  /api/sectors                      — active sectors (any signed-in user)
///   GET  /api/admin/sectors                — all sectors + aliases (admin)
///   POST /api/admin/sectors                — create a sector (admin)
///   PUT  /api/admin/sectors/{id}           — rename / deactivate (admin)
///   DELETE /api/admin/sectors/{id}         — delete (only when unreferenced)
///   POST /api/admin/sectors/{id}/aliases   — add aliases (admin)
///   DELETE /api/admin/sectors/{sid}/aliases/{aid} — remove an alias (admin)
///   POST /api/admin/sync/scraped-jobs      — import + normalize scraped jobs (admin)
/// </summary>
public static class SectorEndpoints
{
    public static IEndpointRouteBuilder MapSectorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sectors").WithTags("Sectors");
        group.MapGet("/", ListSectorsAsync).RequireRateLimiting("jobs_read").WithOpenApi();

        var admin = app.MapGroup("/api/admin/sectors").WithTags("Sectors (admin)");
        admin.MapGet("/", ListAdminSectorsAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        admin.MapPost("/", CreateSectorAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapPut("/{id:guid}", UpdateSectorAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapDelete("/{id:guid}", DeleteSectorAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapPost("/{id:guid}/aliases", AddAliasesAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        admin.MapDelete("/{sectorId:guid}/aliases/{aliasId:guid}", RemoveAliasAsync).RequireRateLimiting("jobs_write").WithOpenApi();

        app.MapPost("/api/admin/sync/scraped-jobs", SyncScrapedJobsAsync)
            .RequireRateLimiting("jobs_write").WithTags("Sectors (admin)").WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record SectorResponse(Guid Id, string Name, string Slug, bool IsActive);

    public sealed record SectorDetailResponse(
        Guid Id, string Name, string Slug, bool IsActive,
        int JobCount, List<AliasResponse> Aliases);

    public sealed record AliasResponse(Guid Id, string Alias);

    public sealed record SectorWriteRequest(string? Name, bool? IsActive, string? Slug);

    public sealed record AddAliasesRequest(List<string> Aliases);

    // -------------------------------------------------------------- Handlers

    /// <summary>GET /api/sectors — the pickers' vocabulary (active only).</summary>
    [Authorize]
    private static async Task<IResult> ListSectorsAsync(ApplicationDbContext db)
    {
        var sectors = await db.Sectors.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SectorResponse(s.Id, s.Name, s.Slug, s.IsActive))
            .ToListAsync();
        return Results.Ok(sectors);
    }

    /// <summary>GET /api/admin/sectors — everything, with aliases and job counts.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> ListAdminSectorsAsync(ApplicationDbContext db)
    {
        var sectors = await db.Sectors.AsNoTracking()
            .Include(s => s.Aliases)
            .OrderBy(s => s.Name)
            .ToListAsync();
        var counts = await db.Jobs.AsNoTracking()
            .Where(j => j.SectorId != null)
            .GroupBy(j => j.SectorId!.Value)
            .Select(g => new { SectorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SectorId, x => x.Count);

        return Results.Ok(sectors.Select(s => new SectorDetailResponse(
            s.Id, s.Name, s.Slug, s.IsActive,
            counts.TryGetValue(s.Id, out var c) ? c : 0,
            s.Aliases.OrderBy(a => a.Alias)
                .Select(a => new AliasResponse(a.Id, a.Alias))
                .ToList())));
    }

    /// <summary>POST /api/admin/sectors — create a sector (name required; slug auto-generated).</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> CreateSectorAsync(
        SectorWriteRequest request, ApplicationDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.Problem("Name is required.", statusCode: StatusCodes.Status400BadRequest);
        }
        var name = request.Name.Trim();
        if (await db.Sectors.AnyAsync(s => s.Name == name))
        {
            return Results.Problem($"A sector named '{name}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slugify(name)
            : Slugify(request.Slug);

        var sector = new Sector { Id = Guid.NewGuid(), Name = name, Slug = slug };
        db.Sectors.Add(sector);
        await db.SaveChangesAsync();
        return Results.Created($"/api/admin/sectors/{sector.Id}", new SectorResponse(sector.Id, sector.Name, sector.Slug, sector.IsActive));
    }

    /// <summary>PUT /api/admin/sectors/{id} — rename and/or deactivate.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> UpdateSectorAsync(
        Guid id, SectorWriteRequest request, ApplicationDbContext db)
    {
        var sector = await db.Sectors.FirstOrDefaultAsync(s => s.Id == id);
        if (sector is null)
        {
            return Results.NotFound();
        }
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            if (await db.Sectors.AnyAsync(s => s.Name == name && s.Id != id))
            {
                return Results.Problem($"A sector named '{name}' already exists.",
                    statusCode: StatusCodes.Status409Conflict);
            }
            sector.Name = name;
        }
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            sector.Slug = Slugify(request.Slug);
        }
        if (request.IsActive is not null)
        {
            sector.IsActive = request.IsActive.Value;
        }
        sector.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new SectorResponse(sector.Id, sector.Name, sector.Slug, sector.IsActive));
    }

    /// <summary>DELETE /api/admin/sectors/{id} — only when nothing references it.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> DeleteSectorAsync(Guid id, ApplicationDbContext db)
    {
        var sector = await db.Sectors.FirstOrDefaultAsync(s => s.Id == id);
        if (sector is null)
        {
            return Results.NotFound();
        }
        if (await db.Jobs.AnyAsync(j => j.SectorId == id))
        {
            return Results.Problem(
                "Sectors that have jobs can't be deleted — deactivate it instead.",
                statusCode: StatusCodes.Status409Conflict);
        }
        db.Sectors.Remove(sector);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    /// <summary>POST /api/admin/sectors/{id}/aliases — add raw names that map here.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> AddAliasesAsync(
        Guid id, AddAliasesRequest request, ApplicationDbContext db)
    {
        var sector = await db.Sectors.FirstOrDefaultAsync(s => s.Id == id);
        if (sector is null)
        {
            return Results.NotFound();
        }

        var added = new List<SectorAlias>();
        foreach (var raw in request.Aliases ?? [])
        {
            var alias = SectorSeedData.Normalize(raw);
            if (alias.Length == 0)
            {
                continue;
            }
            if (await db.SectorAliases.AnyAsync(a => a.Alias == alias))
            {
                continue; // already mapped somewhere
            }
            var entity = new SectorAlias { Id = Guid.NewGuid(), SectorId = id, Alias = alias };
            db.SectorAliases.Add(entity);
            added.Add(entity);
        }
        await db.SaveChangesAsync();
        return Results.Ok(added.Select(a => new AliasResponse(a.Id, a.Alias)).ToList());
    }

    /// <summary>DELETE /api/admin/sectors/{sectorId}/aliases/{aliasId}.</summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> RemoveAliasAsync(
        Guid sectorId, Guid aliasId, ApplicationDbContext db)
    {
        var alias = await db.SectorAliases
            .FirstOrDefaultAsync(a => a.Id == aliasId && a.SectorId == sectorId);
        if (alias is null)
        {
            return Results.NotFound();
        }
        db.SectorAliases.Remove(alias);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // ----------------------------------------------------- Scraped-job sync

    /// <summary>
    /// POST /api/admin/sync/scraped-jobs — import published jobs from the
    /// scraper database into SeraGo's Jobs table. All logic lives in
    /// <see cref="ScrapedJobSyncService"/> (also used by the auto-sync
    /// scheduler); this endpoint is the manual "do it now" trigger.
    /// Re-runs are idempotent: existing (SourceName, ExternalId) pairs are
    /// updated in place, never duplicated.
    /// </summary>
    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> SyncScrapedJobsAsync(
        ScrapedJobSyncService syncService,
        CancellationToken ct)
    {
        var result = await syncService.RunAsync(ct);
        return result is null
            ? Results.Problem(
                "Scraper database is not configured (set DB_HOST/DB_NAME/DB_USER/DB_PASSWORD "
                + "in .env(SeraGo-Scraper)).",
                statusCode: StatusCodes.Status400BadRequest)
            : Results.Ok(result);
    }

    // --------------------------------------------------------------- Helpers

    private static string Slugify(string value)
    {
        var slug = value.ToLowerInvariant()
            .Replace("&", "and")
            .Replace("/", " ")
            .Replace("\\", " ");
        var builder = new System.Text.StringBuilder();
        var lastDash = false;
        foreach (var ch in slug)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastDash = false;
            }
            else if (!lastDash && builder.Length > 0)
            {
                builder.Append('-');
                lastDash = true;
            }
        }
        return builder.ToString().Trim('-');
    }

    private const string SyncQuery = """
        SELECT
            i.external_id, i.title, i.description, i.company, i.location,
            i.job_type, i.url, i.salary, i.published_at, i.deadline, i.is_active,
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
        """;
}
