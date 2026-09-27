using Aufy.Core.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Auth;

/// <summary>
/// Hooks into Aufy's signup endpoint to copy profile fields onto the user.
/// Every self-service signup creates a Talent account — the recruiter role has
/// been removed, and "Admin" is never self-service.
/// </summary>
public class SeraGoSignUpExtension(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext)
    : ISignUpEndpointEvents<ApplicationUser, SeraGoSignUpRequest>
{
    public Task<ProblemHttpResult?> UserCreatingAsync(
        SeraGoSignUpRequest model, HttpRequest httpRequest, ApplicationUser user)
    {
        user.FirstName = model.FirstName?.Trim() ?? string.Empty;
        user.LastName = model.LastName?.Trim() ?? string.Empty;
        user.UserType = UserType.Talent;
        return Task.FromResult<ProblemHttpResult?>(null);
    }

    public async Task UserCreatedAsync(SeraGoSignUpRequest model, HttpRequest httpRequest, ApplicationUser user)
    {
        var result = await userManager.AddToRoleAsync(user, Roles.Talent);
        if (!result.Succeeded)
        {
            // Never leave a half-created account: roll back the user and fail the request.
            await userManager.DeleteAsync(user);
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign role '{Roles.Talent}' to {user.Email}: {errors}");
        }

        // Create the talent profile row (fields stay empty until the user
        // completes their profile) so profile endpoints always find a row.
        // If this fails, roll the user back too — never leave a half-created account.
        try
        {
            dbContext.TalentProfiles.Add(new TalentProfile { UserId = user.Id });
            await dbContext.SaveChangesAsync();
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }
    }
}
