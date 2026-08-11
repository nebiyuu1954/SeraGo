using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Account lifecycle endpoints for the current user:
///
///   POST   /api/account/deactivate — soft disable (IsActive = false).
///          Also blocks password sign-in via Identity's lockout and revokes
///          refresh tokens, so the account can't re-authenticate after the
///          access token expires. Reversible in principle, but there is no
///          reactivation endpoint yet — restoring an account currently needs
///          an admin action or direct DB change (clear LockoutEnd, set
///          IsActive = true).
///   DELETE /api/account             — permanent deletion. Requires the
///          account password as confirmation. Profile rows and the Identity
///          tables (roles, claims, logins, tokens) cascade off AspNetUsers;
///          refresh tokens are deleted explicitly because that table has no
///          foreign key.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account").WithTags("Account");

        group.MapPost("/deactivate", DeactivateAccountAsync).WithOpenApi();
        group.MapDelete("/", DeleteAccountAsync).WithOpenApi();
        group.MapPost("/password/set", SetPasswordAsync).WithOpenApi();

        return app;
    }

    public sealed record DeleteAccountRequest(string Password);
    public sealed record SetPasswordRequest(string Password);

    [Authorize]
    private static async Task<IResult> DeactivateAccountAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        // Self-healing: also run when the account is already inactive but was
        // never fully disabled (e.g. IsActive flipped by another path without
        // the lockout), so deactivate always ends in a locked-out state.
        if (user.IsActive || user.LockoutEnd is null)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            // Block password sign-in. Identity's sign-in flow checks
            // IsLockedOut() before the password regardless of lockoutOnFailure,
            // so an active lockout always refuses the sign-in — this is the
            // standard "disabled account" pattern.
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            await RevokeRefreshTokensAsync(db, user.Id);
        }

        await userManager.UpdateAsync(user);
        return Results.Ok(new { message = "Account deactivated. Sign out and discard any stored tokens." });
    }

    [Authorize]
    private static async Task<IResult> DeleteAccountAsync(
        [FromBody] DeleteAccountRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        // Irreversible action: confirm the caller knows the account password.
        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Results.Problem(
                "Incorrect password. Account was not deleted.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await RevokeRefreshTokensAsync(db, user.Id);

        // TalentProfile/RecruiterProfile and the Identity tables
        // (roles, claims, logins, tokens) cascade off AspNetUsers.
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Results.Problem(
                "Could not delete the account: " + string.Join(", ", result.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Sets a password on an account that doesn't have one yet (e.g. a Google
    /// sign-up). Once set, the user can sign in with email + password on any
    /// device and use the regular forgot-password flow.
    ///
    /// POST /api/account/password/set — authenticated.
    /// </summary>
    [Authorize]
    private static async Task<IResult> SetPasswordAsync(
        [FromBody] SetPasswordRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Problem(
                "Password is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Defense-in-depth: this endpoint is guarded only by the bearer token,
        // so require a confirmed email before letting a session plant a
        // password. Google-created accounts are always EmailConfirmed = true
        // (the OAuth handshake confirms it), so the legitimate flow is never
        // blocked — this only stops unconfirmed/abandoned sessions.
        if (!user.EmailConfirmed)
        {
            return Results.Problem(
                "Confirm your email address before setting a password.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Fails with "User already has a password." when one exists — for
        // changing an existing password use the Aufy change-password endpoint.
        var result = await userManager.AddPasswordAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Results.Problem(
                string.Join(", ", result.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok();
    }

    /// <summary>AufyRefreshTokens has no FK to AspNetUsers — remove rows explicitly.</summary>
    private static Task<int> RevokeRefreshTokensAsync(ApplicationDbContext db, string userId) =>
        db.RefreshTokens.Where(rt => rt.UserId == userId).ExecuteDeleteAsync();
}
