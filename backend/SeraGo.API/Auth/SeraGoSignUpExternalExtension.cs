using Aufy.Core.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Auth;

/// <summary>
/// Hooks into Aufy's external (Google) signup endpoint to copy the display
/// name and assign the chosen role — mirroring
/// <see cref="SeraGoSignUpExtension"/> for the email/password path. The role
/// is validated against the shared whitelist, so "Admin" can never be
/// requested through Google signup either.
/// </summary>
public class SeraGoSignUpExternalExtension(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext)
    : ISignUpExternalEndpointEvents<ApplicationUser, SeraGoSignUpExternalRequest>
{
    public Task<ProblemHttpResult?> UserCreatingAsync(
        SeraGoSignUpExternalRequest model, HttpRequest httpRequest, ApplicationUser user)
    {
        var role = ResolveRole(model.Role);
        if (role is null)
        {
            return Task.FromResult<ProblemHttpResult?>(TypedResults.Problem(
                $"Role must be one of: {string.Join(", ", Roles.SelfService)}",
                statusCode: StatusCodes.Status400BadRequest));
        }

        user.FirstName = model.FirstName?.Trim() ?? string.Empty;
        user.LastName = model.LastName?.Trim() ?? string.Empty;
        user.UserType = role == Roles.Recruiter ? UserType.Recruiter : UserType.Talent;
        return Task.FromResult<ProblemHttpResult?>(null);
    }

    public async Task UserCreatedAsync(
        SeraGoSignUpExternalRequest model, HttpRequest httpRequest, ApplicationUser user)
    {
        var role = ResolveRole(model.Role);
        if (role is null)
        {
            return;
        }

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            // Never leave a half-created account: roll back the user and fail the request.
            await userManager.DeleteAsync(user);
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign role '{role}' to {user.Email}: {errors}");
        }

        // Create the role-appropriate profile row (fields stay empty until the
        // user completes their profile) so profile endpoints always find a row.
        try
        {
            if (role == Roles.Recruiter)
            {
                dbContext.RecruiterProfiles.Add(new RecruiterProfile { UserId = user.Id });
            }
            else
            {
                dbContext.TalentProfiles.Add(new TalentProfile { UserId = user.Id });
            }
            await dbContext.SaveChangesAsync();
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }
    }

    private static string? ResolveRole(string? role) =>
        Roles.SelfService.FirstOrDefault(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
}
