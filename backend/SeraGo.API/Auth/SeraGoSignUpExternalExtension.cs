using System.Security.Claims;
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
/// name and create a Talent account — mirroring
/// <see cref="SeraGoSignUpExtension"/> for the email/password path. The
/// recruiter role has been removed, and "Admin" is never self-service.
/// </summary>
public class SeraGoSignUpExternalExtension(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext)
    : ISignUpExternalEndpointEvents<ApplicationUser, SeraGoSignUpExternalRequest>
{
    public Task<ProblemHttpResult?> UserCreatingAsync(
        SeraGoSignUpExternalRequest model, HttpRequest httpRequest, ApplicationUser user)
    {
        // Aufy sets UserName to Google's display name, but the login endpoint
        // (PasswordSignInAsync) looks users up by UserName — so it must be the
        // email, otherwise email+password login could never find this account
        // ("Invalid email or password" even with the correct password).
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            user.UserName = user.Email;
        }

        // The account is created WITH a password: the user chooses one on the
        // role step so they can also sign in with email + password on any
        // device (not just "Continue with Google"). Validated here because
        // UserManager.CreateAsync(user) without a password never runs Identity's
        // password validators.
        var passwordError = ValidatePassword(model.Password);
        if (passwordError is not null)
        {
            return Task.FromResult<ProblemHttpResult?>(TypedResults.Problem(
                passwordError, statusCode: StatusCodes.Status400BadRequest));
        }
        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, model.Password!);

        // Google already knows the name — prefer its claims so the UI only has
        // to ask for a password. Falls back to the (optional) payload values, and
        // to the full-name claim when given/family names are missing.
        var claims = httpRequest.HttpContext.User;
        user.FirstName = claims.FindFirstValue(ClaimTypes.GivenName)
            ?? claims.FindFirstValue(ClaimTypes.Name)
            ?? model.FirstName?.Trim()
            ?? string.Empty;
        user.LastName = claims.FindFirstValue(ClaimTypes.Surname)
            ?? model.LastName?.Trim()
            ?? string.Empty;
        user.UserType = UserType.Talent;
        return Task.FromResult<ProblemHttpResult?>(null);
    }

    /// <summary>
    /// Mirrors ASP.NET Core Identity's default password policy AND the
    /// frontend's <c>meetsPasswordRules</c> (they must stay in sync): min 6
    /// chars, one uppercase, one lowercase, one digit or symbol.
    /// </summary>
    private static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return "Please choose a password.";
        }
        // Upper bound matches the [StringLength(100)] contract on the request
        // (DataAnnotations aren't auto-enforced by .NET 8 minimal APIs).
        if (password.Length < 6 || password.Length > 100 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            (!password.Any(char.IsDigit) && !password.Any(c => !char.IsLetterOrDigit(c))))
        {
            return "Password does not meet all requirements.";
        }
        return null;
    }

    public async Task UserCreatedAsync(
        SeraGoSignUpExternalRequest model, HttpRequest httpRequest, ApplicationUser user)
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
