using Aufy.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Data;

namespace SeraGo.Infrastructure.Context;

/// <summary>
/// EF Core context. AufyDbContext&lt;TUser&gt; provides the Identity tables
/// (AspNetUsers, AspNetRoles, ...) plus the AufyRefreshTokens table.
/// The role profiles (TalentProfile, RecruiterProfile) are the 1:1 user
/// profile layer — one row per user, shared primary key with AspNetUsers.
/// Jobs is SeraGo's own table (fully owned by EF migrations) — it has no
/// relationship to the scraper's models.
/// </summary>
public class ApplicationDbContext : AufyDbContext<ApplicationUser>
{
    public DbSet<TalentProfile> TalentProfiles => Set<TalentProfile>();
    public DbSet<RecruiterProfile> RecruiterProfiles => Set<RecruiterProfile>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<SavedJob> SavedJobs => Set<SavedJob>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<SectorAlias> SectorAliases => Set<SectorAlias>();
    public DbSet<SyncState> SyncState => Set<SyncState>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1:1 with the owning user, sharing the user's PK (no surrogate Id column).
        modelBuilder.Entity<TalentProfile>(entity =>
        {
            entity.HasKey(p => p.UserId);
            entity.HasOne(p => p.User)
                .WithOne(u => u.TalentProfile)
                .HasForeignKey<TalentProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecruiterProfile>(entity =>
        {
            entity.HasKey(p => p.UserId);
            entity.HasOne(p => p.User)
                .WithOne(u => u.RecruiterProfile)
                .HasForeignKey<RecruiterProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SeraGo's own jobs table. Deleting a user must not delete their job
        // history, so the FK to the poster is restricted.
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasOne(j => j.PostedBy)
                .WithMany()
                .HasForeignKey(j => j.PostedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(j => j.PostedByUserId);
            entity.HasIndex(j => j.Status);
            entity.HasIndex(j => j.IsActive);

            // Source-attributed (scraped) jobs: a unique (source, external_id)
            // pair is the idempotency key for imports — re-importing never
            // duplicates. Postgres treats NULLs as distinct, so recruiter-posted
            // jobs (both null) never collide.
            entity.HasIndex(j => new { j.SourceName, j.ExternalId }).IsUnique();
            entity.HasIndex(j => j.SourceName);

            entity.HasOne(j => j.Sector)
                .WithMany()
                .HasForeignKey(j => j.SectorId)
                .OnDelete(DeleteBehavior.Restrict); // don't delete sectors that have jobs
            entity.HasIndex(j => j.SectorId);

            // The "For you" feed filters on the talent's preferred sectors —
            // a covering index keeps it fast as the jobs table grows.
            entity.HasIndex(j => new { j.SectorId, j.Status, j.IsActive });

            // The public feed filters Status=Published AND IsActive and sorts
            // newest-first — the (IsActive, Status) prefix narrows the scan
            // to live jobs, and the DESC PublishedAt tail serves the sort, so
            // the main talent feed never walks or sorts the whole table as
            // Jobs grows.
            entity.HasIndex(j => new { j.IsActive, j.Status, j.PublishedAt })
                .IsDescending(false, false, true);

            // Npgsql only accepts offset-0 (UTC) DateTimeOffset for timestamptz
            // columns, but clients may send any offset (e.g. "+03:00" for Addis
            // Ababa). Normalize to UTC on write; nulls pass through untouched.
            // Pure client-side mapping — no schema change.
            var utcDateTimeOffset = new ValueConverter<DateTimeOffset, DateTimeOffset>(
                v => v.ToUniversalTime(),
                v => v);
            entity.Property(j => j.PublishedAt).HasConversion(utcDateTimeOffset);
            entity.Property(j => j.Deadline).HasConversion(utcDateTimeOffset);
        });

        // A user's saved jobs — a lightweight snapshot that survives the job's
        // lifecycle deletion (see SavedJob). Deleting a user removes their
        // saves; deleting a job (weekly lifecycle cleanup) nulls the link but
        // keeps the snapshot row for the saved-count stat.
        modelBuilder.Entity<SavedJob>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Job)
                .WithMany()
                .HasForeignKey(s => s.JobId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(s => s.UserId);
            // Postgres treats NULLs as distinct, so a user can hold one row
            // per job — and a single dangling snapshot after its job is gone.
            entity.HasIndex(s => new { s.UserId, s.JobId }).IsUnique();
        });

        // Canonical sectors + their source-name aliases (the standardization
        // vocabulary). Both tables are reference data, seeded at migration time.
        modelBuilder.Entity<Sector>(entity =>
        {
            entity.HasIndex(s => s.Name).IsUnique();
            entity.HasIndex(s => s.Slug).IsUnique();
            entity.HasIndex(s => s.IsActive);

            entity.HasData(SectorSeedData.ToEntities());
        });

        modelBuilder.Entity<SectorAlias>(entity =>
        {
            entity.HasOne(a => a.Sector)
                .WithMany(s => s.Aliases)
                .HasForeignKey(a => a.SectorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => a.Alias).IsUnique();
            entity.HasIndex(a => a.SectorId);

            entity.HasData(SectorSeedData.AliasEntities());
        });

        // Single-row cursor (Id = 1) for the scraped-job sync; see SyncState.
        modelBuilder.Entity<SyncState>(entity =>
        {
            entity.HasKey(s => s.Id);
        });
    }
}
