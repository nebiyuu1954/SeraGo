using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Services;

/// <summary>
/// Weekly lifecycle cleanup: deletes Jobs whose visibility window
/// (deadline + 7 days) has ended. Runs from <see cref="SyncScheduler"/> on
/// Sundays (same slot as the scraper's archive run). Past-window sourced
/// jobs are also removed — they predate the scraper's default-deadline rule
/// and the sync now skips them, so they would otherwise linger with a null
/// deadline forever. Recruiter-posted jobs (SourceName "SeraGo") with no
/// deadline are untouched — they have no lifecycle window. SavedJob snapshot
/// rows survive via the SetNull FK, so the saved-count stat is preserved.
/// </summary>
public sealed class JobLifecycleCleanupService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobLifecycleCleanupService> _logger;

    public JobLifecycleCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<JobLifecycleCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Deletes past-window Jobs; returns how many rows were removed.</summary>
    public async Task<int> CleanupAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(-JobLifecycle.GraceDays);
        // Sourced jobs that predate the default-deadline rule (null deadline,
        // old, not recruiter-posted) are cleaned too — they will never get a
        // real deadline because the sync skips past-window rows.
        var staleCutoff = now.AddDays(-(JobLifecycle.GraceDays + 60));

        var toDelete = await db.Jobs
            .Where(j =>
                (j.Deadline != null && j.Deadline < cutoff)
                || (j.Deadline == null
                    && j.SourceName != null
                    && j.SourceName != "SeraGo"
                    && j.CreatedAt < staleCutoff))
            .ToListAsync(ct);
        if (toDelete.Count == 0)
        {
            return 0;
        }

        db.Jobs.RemoveRange(toDelete);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Lifecycle cleanup deleted {Count} past-window job(s) (deadline + {Grace}d passed).",
            toDelete.Count, JobLifecycle.GraceDays);
        return toDelete.Count;
    }
}
