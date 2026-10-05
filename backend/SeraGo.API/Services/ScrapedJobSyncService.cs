using System.Data.Common;
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
/// Imports published jobs from the scraper's <c>core_normalizedjob</c> table
/// into SeraGo's Jobs table. NormalizedJob is the source-agnostic contract
/// written by every scraper's <c>_normalize_for_export()</c> — the .NET sync
/// reads this single flat table instead of the old 5-way LEFT JOIN across
/// per-site models, so adding a new website is a pure Python concern.
///
/// Re-runs are idempotent — existing (SourceName, ExternalId) pairs are
/// updated in place, never duplicated — and concurrent triggers are
/// serialized by an internal gate, so overlapping runs are safe.
///
/// The first run is a full bootstrap; later runs are INCREMENTAL — only
/// items whose scraper updated_at is newer than the stored cursor (see
/// <see cref="SyncState"/>) are pulled, so the cost scales with what
/// changed, not with how much data exists.
///
/// Lifecycle: singleton. All scoped services (DbContext, AI client, save
/// service) are resolved from a scope created per-run, so this singleton can
/// safely be consumed by the hosted SyncScheduler without captive dependency
/// issues.
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
    public async Task<SyncResult?> RunAsync(string triggeredBy = "manual", CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return await RunCoreAsync(triggeredBy, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SyncResult?> RunCoreAsync(string triggeredBy, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var options = scope.ServiceProvider.GetRequiredService<ScraperDbOptions>();
        var aiClient = scope.ServiceProvider.GetRequiredService<AiClassificationClient>();
        var saveService = scope.ServiceProvider.GetRequiredService<AiClassificationSaveService>();

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
        var seenKeys = new HashSet<string>();
        DateTime? maxUpdatedAt = null;

        // Load every existing scraped-sourced job ONCE, then match in memory.
        var existingJobs = await db.Jobs
            .Where(j => j.SourceName != null && j.ExternalId != null)
            .ToListAsync(ct);
        var jobsByKey = new Dictionary<(string SourceName, string ExternalId), Job>(existingJobs.Count);
        foreach (var existing in existingJobs)
        {
            jobsByKey[(existing.SourceName!, existing.ExternalId!)] = existing;
        }

        // Jobs that were actually inserted or updated in this sync run. Only
        // these get sent to the AI classification service — unchanged jobs
        // (which include already-classified ones) are never re-sent, so a
        // routine sync doesn't re-classify everything and burn LLM calls.
        var touchedIds = new HashSet<Guid>(jobsByKey.Count);

        // Read from the NormalizedJob table — the source-agnostic contract.
        // No more 5-way LEFT JOIN across per-site models.
        await using var conn = new NpgsqlConnection(options.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = isBootstrap ? SyncQueryBootstrap : SyncQueryIncremental;
        if (!isBootstrap)
        {
            cmd.Parameters.AddWithValue("watermark", watermark!.Value);
        }
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
                if (jobsByKey.TryGetValue((sourceName, externalId), out var gone) && gone.IsActive)
                {
                    gone.IsActive = false;
                    gone.UpdatedAt = DateTimeOffset.UtcNow;
                    deactivated++;
                }
                continue;
            }

            if (isBootstrap)
            {
                seenKeys.Add($"{sourceName}|{externalId}");
            }

            // Sector resolution: the NormalizedJob carries the raw sector_name;
            // we resolve it through aliases, then fall back to title classification.
            var rawSector = ReadString(reader, "sector_name");
            var title = ReadString(reader, "title") ?? string.Empty;
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
            var description = ReadString(reader, "description") ?? string.Empty;
            var company = ReadString(reader, "company") ?? string.Empty;
            var location = ReadString(reader, "location") ?? string.Empty;
            var url = ReadString(reader, "url") ?? string.Empty;
            var salary = ReadString(reader, "salary") ?? string.Empty;
            var logo = ReadString(reader, "company_logo_url");
            logo = string.IsNullOrWhiteSpace(logo) ? null : logo;
            var experience = ReadString(reader, "experience_level");
            var workMode = MapWorkMode(ReadString(reader, "work_mode"));
            var skills = ReadJsonArray(reader, "skills");

            // Source-specific fields.
            var afriworkSectors = ReadJsonArray(reader, "afriwork_sectors");
            var compCents = ReadNullableInt(reader, "compensation_amount_cents");
            var compType = ReadString(reader, "compensation_type");
            var compCurrency = ReadString(reader, "compensation_currency");
            var entityType = ReadString(reader, "entity_type");
            var ethioCategories = ReadJsonArray(reader, "ethio_categories");
            var appMethod = ReadString(reader, "ethio_app_method") ?? ReadString(reader, "hahu_app_method");
            var appEmail = ReadString(reader, "ethio_app_email") ?? ReadString(reader, "hahu_app_email");
            var appUrl = ReadString(reader, "ethio_career_link") ?? ReadString(reader, "hahu_app_url");
            var hahuLogo = ReadString(reader, "hahu_entity_logo");
            var hahuSector = ReadString(reader, "hahu_sector");
            var hahuSubSector = ReadString(reader, "hahu_sub_sector");

            // SourceSectors = every raw listing sector label this job carries
            // (universal sector_name + Afriwork sectors + HaHu sector/sub-sector),
            // deduped. The AI classifier records these as SubSector aliases, so
            // the vocabulary grows from real listing text instead of staying
            // empty.
            var sourceSectors = MergeSourceSectors(
                rawSector, afriworkSectors, hahuSector, hahuSubSector);
            var hahuUpstream = ReadString(reader, "hahu_upstream");
            var areaName = ReadString(reader, "area_name");
            var hahuExp = ReadNullableInt(reader, "hahu_exp");
            var numApplicants = ReadNullableInt(reader, "number_of_applicants");
            var hahuSalary = ReadNullableDecimal(reader, "hahu_salary");
            var geezLogo = ReadString(reader, "geez_logo");
            var employmentText = ReadString(reader, "employment_text");
            var jobTime = ReadString(reader, "job_time");
            var geezJobType = ReadString(reader, "geez_job_type");
            var experienceText = ReadString(reader, "experience_text");
            var minExp = ReadNullableInt(reader, "min_experience_years");
            var maxExp = ReadNullableInt(reader, "max_experience_years");
            var geezPosted = ReadString(reader, "geez_posted");
            var deadlineText = ReadString(reader, "deadline_text");
            var reporterTypeText = ReadString(reader, "reporter_type_text");
            var reporterPosted = ReadString(reader, "reporter_posted");
            var refreshedAt = ReadDateTime(reader, "refreshed_at");

            // Resolve the best company logo: prefer per-site logos over the universal one.
            var bestLogo = logo;
            if (string.IsNullOrWhiteSpace(bestLogo) && !string.IsNullOrWhiteSpace(hahuLogo)) bestLogo = hahuLogo;
            if (string.IsNullOrWhiteSpace(bestLogo) && !string.IsNullOrWhiteSpace(geezLogo)) bestLogo = geezLogo;

            // Resolve experience level: prefer the universal one, fall back to HaHu years.
            var bestExperience = experience;
            if (string.IsNullOrWhiteSpace(bestExperience) && hahuExp is not null)
            {
                bestExperience = MapExperienceLevel(hahuExp.Value);
            }

            // Resolve salary: prefer the universal text, fall back to HaHu decimal.
            var bestSalary = salary;
            if (string.IsNullOrWhiteSpace(bestSalary) && hahuSalary is not null)
            {
                bestSalary = $"{hahuSalary.Value:,.0f} ETB monthly";
            }

            // Compare EVERY imported field.
            var isSame = job is not null
                && job.Title == title
                && job.Description == description
                && job.Company == company
                && job.Location == location
                && job.JobType == jobType
                && job.Url == url
                && job.Salary == bestSalary
                && job.PublishedAt == publishedAt
                && job.Deadline == deadline
                && job.SectorId == sector?.Id
                && job.SectorName == sector?.Name
                && job.CompanyLogoUrl == bestLogo
                && job.ExperienceLevel == bestExperience
                && job.WorkMode == workMode
                && job.Skills == skills
                && job.SourceSectors == sourceSectors
                && job.CompensationAmountCents == compCents
                && job.CompensationType == compType
                && job.CompensationCurrency == compCurrency
                && job.EntityType == entityType
                && job.SourceCategories == ethioCategories
                && job.ApplicationMethod == appMethod
                && job.ApplicationEmail == appEmail
                && job.ApplicationUrl == appUrl
                && job.UpstreamSource == hahuUpstream
                && job.AreaName == areaName
                && job.SubSectorName == hahuSubSector
                && job.NumberOfApplicants == numApplicants
                && job.EmploymentText == employmentText
                && job.JobTime == jobTime
                && job.SiteJobType == geezJobType
                && job.ExperienceText == experienceText
                && job.MaxExperienceYears == maxExp
                && job.PostedText == (geezPosted ?? reporterPosted)
                && job.DeadlineText == deadlineText
                && job.JobTypeText == reporterTypeText
                && job.RefreshedAt == refreshedAt;
            if (job is not null)
            {
                if (isSame)
                {
                    unchanged++;
                    continue;
                }
                job.Title = title;
                job.Description = description;
                job.Company = company;
                job.Location = location;
                job.JobType = jobType;
                job.Url = url;
                job.Salary = bestSalary;
                job.PublishedAt = publishedAt;
                job.Deadline = deadline;
                job.SectorId = sector?.Id;
                job.SectorName = sector?.Name;
                job.CompanyLogoUrl = bestLogo;
                job.ExperienceLevel = bestExperience;
                job.WorkMode = workMode;
                job.Skills = skills;
                job.RefreshedAt = refreshedAt;
                // Source-specific fields
                job.SourceSectors = sourceSectors;
                job.CompensationAmountCents = compCents;
                job.CompensationType = compType;
                job.CompensationCurrency = compCurrency;
                job.EntityType = entityType;
                job.SourceCategories = ethioCategories;
                job.ApplicationMethod = appMethod;
                job.ApplicationEmail = appEmail;
                job.ApplicationUrl = appUrl;
                job.UpstreamSource = hahuUpstream;
                job.AreaName = areaName;
                job.SubSectorName = hahuSubSector;
                job.NumberOfApplicants = numApplicants;
                job.EmploymentText = employmentText;
                job.JobTime = jobTime;
                job.SiteJobType = geezJobType;
                job.ExperienceText = experienceText;
                job.MaxExperienceYears = maxExp;
                job.PostedText = geezPosted ?? reporterPosted;
                job.DeadlineText = deadlineText;
                job.JobTypeText = reporterTypeText;
                job.Status = JobStatus.Published;
                job.UpdatedAt = DateTimeOffset.UtcNow;
                updated++;
                touchedIds.Add(job.Id);
            }
            else
            {
                var newJob = new Job
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Description = description,
                    Company = company,
                    Location = location,
                    JobType = jobType,
                    Url = url,
                    Salary = bestSalary,
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
                    CompanyLogoUrl = bestLogo,
                    SectorId = sector?.Id,
                    SectorName = sector?.Name,
                    ExperienceLevel = string.IsNullOrWhiteSpace(bestExperience) ? null : bestExperience,
                    WorkMode = workMode,
                    Skills = string.IsNullOrWhiteSpace(skills) ? null : skills,
                    RefreshedAt = refreshedAt,
                    // Source-specific fields
                    SourceSectors = sourceSectors,
                    CompensationAmountCents = compCents,
                    CompensationType = compType,
                    CompensationCurrency = compCurrency,
                    EntityType = entityType,
                    SourceCategories = ethioCategories,
                    ApplicationMethod = appMethod,
                    ApplicationEmail = appEmail,
                    ApplicationUrl = appUrl,
                    UpstreamSource = hahuUpstream,
                    AreaName = areaName,
                    SubSectorName = hahuSubSector,
                    NumberOfApplicants = numApplicants,
                    EmploymentText = employmentText,
                    JobTime = jobTime,
                    SiteJobType = geezJobType,
                    ExperienceText = experienceText,
                    MaxExperienceYears = maxExp,
                    PostedText = geezPosted ?? reporterPosted,
                    DeadlineText = deadlineText,
                    JobTypeText = reporterTypeText,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                db.Jobs.Add(newJob);
                jobsByKey[(sourceName, externalId)] = newJob;
                inserted++;
                touchedIds.Add(newJob.Id);
            }

            if ((inserted + updated + unchanged) % 100 == 0)
            {
                await db.SaveChangesAsync(ct);
            }
        }
        await db.SaveChangesAsync(ct);

        // Bootstrap deactivation: hide SeraGo jobs whose listing never appeared
        // in the scraper's active set.
        if (isBootstrap)
        {
            foreach (var job in existingJobs)
            {
                if (!job.IsActive)
                {
                    continue;
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

        // Advance the cursor.
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

        // Persist the run result so the admin dashboard and scraper page
        // can show "last sync" and run history without re-querying the scraper DB.
        db.SyncRuns.Add(new SyncRun
        {
            Id = 0, // auto-increment
            RanAt = DateTime.UtcNow,
            Inserted = inserted,
            Updated = updated,
            Unchanged = unchanged,
            Uncategorized = uncategorized,
            Deactivated = deactivated,
            UnknownSectors = unknownSectors.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(unknownSectors.ToList())
                : null,
            TriggeredBy = triggeredBy,
        });
        await db.SaveChangesAsync(ct);

        // Send the touched jobs to the AI classification service so scraped jobs
        // get a sector immediately instead of waiting for an admin trigger.
        // Best-effort: if the AI service is down the sync still succeeds, and the
        // same jobs will be classified on the next sync run (SubSector dedups make
        // re-sends safe). Only jobs inserted/updated in THIS run are sent —
        // unchanged, already-classified jobs are not re-sent.
        var touchedJobs = jobsByKey.Values.Where(j => touchedIds.Contains(j.Id)).ToList();
        await ClassifyTouchedJobsAsync(db, touchedJobs, aiClient, saveService, ct);

        return new SyncResult(
            inserted, updated, unchanged, uncategorized,
            unknownSectors.ToList(), deactivated);
    }

    /// <summary>
    /// Classify the jobs that were inserted or updated in this sync run via the
    /// decoupled SeraGo-AI service. Only jobs that have a source name (scraped
    /// jobs) are sent; the rest are left for the admin to review.
    /// </summary>
    private async Task ClassifyTouchedJobsAsync(
        ApplicationDbContext db,
        IEnumerable<Job> touchedJobs,
        AiClassificationClient aiClient,
        AiClassificationSaveService saveService,
        CancellationToken ct)
    {
        var jobs = touchedJobs.Where(j => j.SourceName is not null).ToList();
        if (jobs.Count == 0)
        {
            return;
        }

        // Build the request the AI service expects. We also send the job's
        // current sector so the AI service can record the 'before' state in the
        // classify log (original_sector_slug/name).
        var requestJobs = new List<AiClassificationRequestJob>(jobs.Count);
        foreach (var j in jobs)
        {
            var currentSlug = j.Sector?.Slug;
            var currentName = j.Sector?.Name;
            var extraOriginal = (currentSlug is not null || currentName is not null)
                ? new Dictionary<string, string>(2)
                { ["sectorSlug"] = currentSlug ?? "", [
                    "sectorName"] = currentName ?? "" }
                : null;

            requestJobs.Add(new AiClassificationRequestJob(
                JobId: j.Id.ToString(),
                Title: j.Title,
                SourceSectors: ParseSourceSectors(j.SourceSectors),
                Description: j.Description,
                ExtraOriginal: extraOriginal));
        }

        AiClassificationResult[] aiResults;
        try
        {
            var list = await aiClient.ClassifyAsync(requestJobs, ct);
            aiResults = list.ToArray();
        }
        catch (AiClassificationException ex)
        {
            _logger.LogWarning(ex,
                "SeraGo-AI classify failed for {JobCount} scraped jobs — leaving sectors as-is for now",
                jobs.Count);
            aiResults = new AiClassificationResult[jobs.Count];
            for (var i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                aiResults[i] = new AiClassificationResult(
                    JobId: j.Id.ToString(),
                    SectorId: null,
                    SectorName: null,
                    SectorSlug: null,
                    SubSectorName: null,
                    Alias: FirstSourceSector(j.SourceSectors),
                    Confidence: null,
                    Reasoning: null,
                    Uncategorized: true,
                    Error: ex.Message);
            }
        }

        // Apply the results: set SectorId/SectorName from the SubSector decision,
        // store the AI trace, dedup-insert SubSector rows, and run the local
        // SubSector fallback when the LLM failed.
        var outcomes = await saveService.ApplyAsync(jobs, aiResults, ct);
        var assigned = outcomes.Count(o => o.Assigned);
        if (assigned > 0 || aiResults.Any(r => r.Error is not null))
        {
            await db.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Post-sync AI classification: {Jobs} jobs sent, {Assigned} assigned a sector",
            jobs.Count, assigned);

        var unassigned = outcomes.Where(o => !o.Assigned).ToList();
        if (unassigned.Count > 0)
        {
            using var scope = _scopeFactory.CreateScope();
            var telegramService = scope.ServiceProvider.GetService<TelegramBotService>();
            if (telegramService != null)
            {
                var msg = $"⚠️ <b>{unassigned.Count} Jobs Uncategorized</b>\n\n";
                foreach (var o in unassigned.Take(10))
                {
                    var j = jobs.First(x => x.Id.ToString() == o.JobId);
                    msg += $"• {j.Title} (<i>{j.Company}</i>)\n";
                }
                if (unassigned.Count > 10) msg += $"...and {unassigned.Count - 10} more.\n";
                msg += "\n<a href=\"https://serago.pro.et/dashboard/admin/sectors\">Review & Sync</a>";
                await telegramService.SendAdminAlertAsync(msg);
            }
        }
    }

    // --------------------------------------------------------------- Helpers

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

    /// <summary>Tolerant job-type mapping.</summary>
    private static JobType MapJobType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return JobType.Other;
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

    /// <summary>Tolerant work-mode mapping (scraper values: ONSITE/REMOTE/HYBRID).</summary>
    private static WorkMode MapWorkMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return WorkMode.Onsite;
        }
        var text = value.ToLowerInvariant();
        if (text.Contains("remote")) return WorkMode.Remote;
        if (text.Contains("hybrid")) return WorkMode.Hybrid;
        return WorkMode.Onsite;
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
        if (value.Kind == DateTimeKind.Unspecified)
        {
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        return new DateTimeOffset(value);
    }

    private static int? ReadNullableInt(DbDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        return reader.IsDBNull(i) ? null : reader.GetInt32(i);
    }

    private static decimal? ReadNullableDecimal(DbDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        return reader.IsDBNull(i) ? null : reader.GetDecimal(i);
    }

    /// <summary>Read a JSONB array column and return it as a JSON string, or null.</summary>
    /// <summary>
    /// Merge the raw listing sector labels from every source-specific field into
    /// one deduped JSON array for Job.SourceSectors. Returns null when the job
    /// carries no sector label at all.
    /// </summary>
    private static string? MergeSourceSectors(
        string? universalSector,
        string? afriworkSectorsJson,
        string? hahuSector,
        string? hahuSubSector)
    {
        var labels = new List<string>();
        void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }
            var trimmed = value.Trim();
            if (!labels.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                labels.Add(trimmed);
            }
        }

        Add(universalSector);
        if (!string.IsNullOrWhiteSpace(afriworkSectorsJson))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(afriworkSectorsJson);
                if (parsed is not null)
                {
                    foreach (var s in parsed)
                    {
                        Add(s);
                    }
                }
            }
            catch
            {
                // Non-array garbage in the column — ignore it.
            }
        }
        Add(hahuSector);
        Add(hahuSubSector);

        return labels.Count == 0 ? null : JsonSerializer.Serialize(labels);
    }

    private static string? ReadJsonArray(DbDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        if (reader.IsDBNull(i))
        {
            return null;
        }
        var value = reader.GetValue(i);
        if (value is string s)
        {
            return s;
        }
        // Npgsql may return it as a JsonDocument or object[].
        return value?.ToString();
    }

    /// <summary>Map years of experience to a human-readable level band.</summary>
    private static string MapExperienceLevel(int years)
    {
        if (years < 1) return "Entry";
        if (years < 3) return "Junior";
        if (years < 5) return "Mid";
        return "Senior";
    }

    /// <summary>Parse the job's JSON-array sourceSectors into a list, or empty.</summary>
    private static List<string> ParseSourceSectors(string? sourceSectors)
    {
        if (string.IsNullOrWhiteSpace(sourceSectors))
        {
            return [];
        }
        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(sourceSectors);
            return parsed ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? FirstSourceSector(string? sourceSectors)
    {
        var list = ParseSourceSectors(sourceSectors);
        return list.Count > 0 ? list[0] : null;
    }

    // --------------------------------------------------------------- Queries
    //
    // We read from scraped_items + per-site models via LEFT JOINs instead of
    // the flat NormalizedJob table. This carries every source-specific field
    // through to the .NET Job model so the frontend can render source-specific
    // layouts (Afriwork skills/sectors, HaHu entity logo, GeezJobs employment
    // text, etc.).

    private const string SyncQueryBase = """
        SELECT
            i.external_id, i.title, i.description, i.company, i.location,
            i.job_type, i.url, i.salary, i.published_at, i.deadline,
            i.is_active, i.updated_at, s.slug AS source_slug,
            -- Universal enriched fields
            i.company_logo_url, i.work_mode, i.experience_level,
            i.sector_name, i.skills,
            -- Afriwork-specific
            a.sectors::text AS afriwork_sectors,
            a.compensation_amount_cents, a.compensation_type,
            a.compensation_currency, a.entity_type,
            a.refreshed_at,
            -- EthioJobs-specific
            e.catalogs::text AS ethio_categories,
            e.application_method AS ethio_app_method,
            e.application_email AS ethio_app_email,
            e.career_page_link AS ethio_career_link,
            e.company AS ethio_company,
            -- HaHuJobs-specific
            h.entity_logo AS hahu_entity_logo,
            h.entity_name AS hahu_entity_name,
            h.sector_name AS hahu_sector,
            h.sub_sector_name AS hahu_sub_sector,
            h.application_method AS hahu_app_method,
            h.application_url AS hahu_app_url,
            h.application_email AS hahu_app_email,
            h.source AS hahu_upstream,
            h.area_name,
            h.years_of_experience AS hahu_exp,
            h.number_of_applicants,
            h.salary AS hahu_salary,
            -- GeezJobs-specific
            g.company_logo AS geez_logo,
            g.employment_text, g.job_time,
            g.job_type AS geez_job_type,
            g.experience_text,
            g.min_experience_years, g.max_experience_years,
            g.posted_text AS geez_posted, g.deadline_text,
            -- ReporterJobs-specific
            r.job_type_text AS reporter_type_text,
            r.posted_text AS reporter_posted
        FROM core_scrapeditem i
        JOIN core_source s ON s.id = i.source_id
        LEFT JOIN core_afriworkjob a ON a.id = i.afriwork_job_id
        LEFT JOIN core_ethiojobsjob e ON e.id = i.ethiojobs_job_id
        LEFT JOIN core_hahujob h ON h.id = i.hahujobs_job_id
        LEFT JOIN core_geezjob g ON g.id = i.geezjobs_job_id
        LEFT JOIN core_reporterjob r ON r.id = i.reporter_job_id
        """;

    /// <summary>First-ever run: every ACTIVE listing from scraped_items + per-site models.</summary>
    private const string SyncQueryBootstrap = SyncQueryBase + @"
        WHERE i.is_active = true
          AND (i.deadline IS NULL OR i.deadline > @lifecycleCutoff)
        ";

    /// <summary>Incremental: only items changed since the cursor.</summary>
    private const string SyncQueryIncremental = SyncQueryBase + @"
        WHERE i.updated_at > @watermark
          AND (i.deadline IS NULL OR i.deadline > @lifecycleCutoff)
        ";
}
