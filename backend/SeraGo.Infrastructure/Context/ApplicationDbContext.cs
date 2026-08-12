using Aufy.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SeraGo.Core.Domain.Entities;

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
    }
}
