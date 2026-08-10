using Aufy.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;

namespace SeraGo.Infrastructure.Context;

/// <summary>
/// EF Core context. AufyDbContext&lt;TUser&gt; provides the Identity tables
/// (AspNetUsers, AspNetRoles, ...) plus the AufyRefreshTokens table.
/// The role profiles (TalentProfile, RecruiterProfile) are the 1:1 user
/// profile layer — one row per user, shared primary key with AspNetUsers.
/// </summary>
public class ApplicationDbContext : AufyDbContext<ApplicationUser>
{
    public DbSet<TalentProfile> TalentProfiles => Set<TalentProfile>();
    public DbSet<RecruiterProfile> RecruiterProfiles => Set<RecruiterProfile>();

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
    }
}
