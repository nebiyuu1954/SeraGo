using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Saved (bookmarked) jobs for a user.
///
/// GET    /api/saved-jobs              — my saved jobs, with live status
///                                       (open / deadlinePassed / removed) and
///                                       the countdown end (deadline + 7d)
/// POST   /api/saved-jobs/{jobId}      — save a visible job (idempotent)
/// DELETE /api/saved-jobs/{jobId}      — unsave
///
/// The saved list shows a job until its lifecycle window ends (deadline + 7
/// days — the same rule as the public feed), even after the source removed it
/// (status "removed" + a countdown so the user knows it is going away). After
/// the window the Job row is deleted by the weekly cleanup, but the SavedJob
/// snapshot row stays, so the "how many jobs I saved" stat is always
/// countable (returned as totalSaved).
/// </summary>
public static class SavedJobEndpoints
{
    public static IEndpointRouteBuilder MapSavedJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/saved-jobs").WithTags("Saved Jobs");

        group.MapGet("/", ListSavedJobsAsync).RequireRateLimiting("jobs_read").WithOpenApi();
        group.MapPost("/{jobId:guid}", SaveJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();
        group.MapDelete("/{jobId:guid}", UnsaveJobAsync).RequireRateLimiting("jobs_write").WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record SavedJobItem(
        Guid SavedJobId,
        Guid? JobId,
        string Title,
        string Company,
        string? SourceName,
        string? SourceUrl,
        string? CompanyLogoUrl,
        string Location,
        string Salary,
        string? Deadline,       // UTC ISO-8601
        string Status,          // open | deadlinePassed | removed
        string? RemovedAt,      // UTC ISO-8601 — when the card leaves the list (deadline + 7d)
        string SavedAt);        // UTC ISO-8601

    public sealed record SavedJobListData(
        List<SavedJobItem> Items,
        int TotalSaved,         // all saved rows, including snapshots of deleted jobs
        int AffectedCount);     // items currently deadlinePassed or removed (for the banner)

    // ------------------------------------------------------------- Handlers

    /// <summary>GET /api/saved-jobs — the caller's saved jobs within their lifecycle window.</summary>
    [Authorize]
    private static async Task<IResult> ListSavedJobsAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var now = DateTimeOffset.UtcNow;
        // The user just opened their saved list — mark it seen (drives the
        // "newly removed" banner on the next visit).
        var saved = await db.SavedJobs
            .Where(s => s.UserId == user.Id && s.SeenAt == null)
            .ToListAsync(ct);
        foreach (var row in saved)
        {
            row.SeenAt = now;
        }

        // Everything the user saved — the stat survives job deletions.
        var totalSaved = await db.SavedJobs.CountAsync(s => s.UserId == user.Id, ct);

        // Cards still within their lifecycle window (job row exists AND its
        // deadline + 7 days hasn't passed) — the same rule as the public feed.
        var window = now.AddDays(-JobLifecycle.GraceDays);
        var items = await db.SavedJobs
            .AsNoTracking()
            .Where(s => s.UserId == user.Id && s.Job != null
                && (s.Job!.Deadline == null || s.Job.Deadline > window))
            .Include(s => s.Job).ThenInclude(j => j!.PostedBy)
            .Select(s => new { Saved = s, Job = s.Job! })
            .OrderByDescending(x => x.Saved.SavedAt)
            .ToListAsync(ct);

        var response = items.Select(x =>
        {
            var job = x.Job;
            var status = job.IsActive
                ? (job.Deadline != null && job.Deadline <= now ? "deadlinePassed" : "open")
                : "removed";
            return new SavedJobItem(
                x.Saved.Id,
                job.Id,
                job.Title,
                job.Company,
                job.SourceName,
                job.SourceUrl,
                NullIfBlank(job.CompanyLogoUrl) ?? NullIfBlank(job.PostedBy?.AvatarUrl) ?? x.Saved.CompanyLogoUrl,
                job.Location,
                job.Salary,
                FormatDate(job.Deadline),
                status,
                FormatDate(JobLifecycle.WindowEnd(job.Deadline, now)),
                FormatDate(x.Saved.SavedAt));
        }).ToList();

        if (saved.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(new SavedJobListData(
            response,
            totalSaved,
            response.Count(i => i.Status is "deadlinePassed" or "removed")));
    }

    /// <summary>POST /api/saved-jobs/{jobId} — save a visible job (idempotent).</summary>
    [Authorize]
    private static async Task<IResult> SaveJobAsync(
        Guid jobId,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var job = await db.Jobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
        {
            return Results.NotFound();
        }
        // Only jobs a user could currently see in the feed can be saved.
        var isVisible = job.Status == JobStatus.Published && job.IsActive
            && JobLifecycle.IsWithinWindow(job.Deadline, DateTimeOffset.UtcNow);
        if (!isVisible)
        {
            return Results.Problem("This job is no longer available to save.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var existing = await db.SavedJobs
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.JobId == jobId, ct);
        if (existing is not null)
        {
            return Results.Ok(); // already saved — idempotent
        }

        db.SavedJobs.Add(new SavedJob
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobId = job.Id,
            Title = job.Title,
            Company = job.Company,
            SourceName = job.SourceName,
            CompanyLogoUrl = job.CompanyLogoUrl,
            SavedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/saved-jobs/{jobId}", null);
    }

    /// <summary>DELETE /api/saved-jobs/{jobId} — unsave.</summary>
    [Authorize]
    private static async Task<IResult> UnsaveJobAsync(
        Guid jobId,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var saved = await db.SavedJobs
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.JobId == jobId, ct);
        if (saved is null)
        {
            return Results.NotFound();
        }

        db.SavedJobs.Remove(saved);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // --------------------------------------------------------------- Helpers

    private const string UtcDateFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static string FormatDate(DateTimeOffset value) =>
        value.ToUniversalTime().ToString(UtcDateFormat, CultureInfo.InvariantCulture);    private static string? FormatDate(DateTimeOffset? value) => value.HasValue ? FormatDate(value.Value) : null;

    /// <summary>Blank strings become null.</summary>
    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
