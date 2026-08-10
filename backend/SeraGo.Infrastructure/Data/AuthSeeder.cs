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
/// Seeds the three roles (Admin, Talent, Recruiter) and the bootstrap Admin
/// account. Also keeps the "every Talent/Recruiter has a profile row"
/// invariant: new signups get one from the signup extension, this repairs
/// accounts created before profiles existed. Runs on startup (see Program.cs).
/// Idempotent — safe to run every boot.
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

        // 2. Backfill: every Talent/Recruiter user must have a profile row.
        var usersMissingProfiles = await userManager.Users
            .Where(u => u.UserType == UserType.Talent || u.UserType == UserType.Recruiter)
            .ToListAsync();

        foreach (var user in usersMissingProfiles)
        {
            if (user.UserType == UserType.Recruiter)
            {
                if (!await dbContext.RecruiterProfiles.AnyAsync(p => p.UserId == user.Id))
                {
                    dbContext.RecruiterProfiles.Add(new RecruiterProfile { UserId = user.Id });
                }
            }
            else if (!await dbContext.TalentProfiles.AnyAsync(p => p.UserId == user.Id))
            {
                dbContext.TalentProfiles.Add(new TalentProfile { UserId = user.Id });
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }

        // 3. Bootstrap admin — only when Admin:Email / Admin:Password are configured.
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

        // 4. Repair: make sure the admin account actually holds the Admin role.
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
