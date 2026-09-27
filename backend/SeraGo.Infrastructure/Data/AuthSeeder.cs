using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.Infrastructure.Data;

/// <summary>
/// Seeds the roles (Admin, Talent) and the bootstrap Admin account. Also keeps
/// the "every Talent has a profile row" invariant: new signups get one from the
/// signup extension, this repairs accounts created before profiles existed.
/// Runs on startup (see Program.cs). Idempotent — safe to run every boot.
///
/// Recruiter accounts (a retired role) are deactivated here so they can no
/// longer sign in.
/// </summary>
public class AuthSeeder(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    ILogger<AuthSeeder> logger,
    ApplicationDbContext dbContext)
{
    public async Task SeedAsync()
    {
        // 1. Roles
        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    logger.LogWarning("Failed to seed role {Role}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // 2. Retire recruiter accounts — the recruiter role no longer exists.
        // Any user still typed as a Recruiter is deactivated and locked out.
        var retiredRecruiters = await userManager.Users
            .Where(u => u.UserType == UserType.Recruiter && u.IsActive)
            .ToListAsync();

        foreach (var user in retiredRecruiters)
        {
            user.IsActive = false;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.UpdatedAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }

        if (retiredRecruiters.Count > 0)
        {
            logger.LogInformation(
                "Deactivated {Count} retired recruiter account(s).", retiredRecruiters.Count);
        }

        // 3. Backfill: every Talent user must have a profile row.
        var talentUsers = await userManager.Users
            .Where(u => u.UserType == UserType.Talent)
            .ToListAsync();

        foreach (var user in talentUsers)
        {
            if (!await dbContext.TalentProfiles.AnyAsync(p => p.UserId == user.Id))
            {
                dbContext.TalentProfiles.Add(new TalentProfile { UserId = user.Id });
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }

        // 4. Bootstrap admin — only when Admin:Email / Admin:Password are configured.
        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "SeraGo",
                LastName = "Admin",
                UserType = UserType.Admin,
                IsActive = true,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to seed admin {Email}: {Errors}",
                    adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        // 5. Repair: make sure the admin account actually holds the Admin role.
        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, Roles.Admin);
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to assign Admin role to {Email}: {Errors}",
                    adminEmail, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }

    }
}
