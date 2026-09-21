using Aufy.Core.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeraGo.API.Services;
using SeraGo.Core.Domain.Entities;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Replacement for Aufy 1.0.0's <c>TokenEndpoint</c> (POST /api/auth/token).
/// The shipped endpoint returns the same generic "Invalid email or password"
/// for EVERY failure — including account lockout — so users get locked out
/// (Identity's 5-attempt / 5-minute policy is on) with no explanation.
///
/// This endpoint mirrors Aufy's flow exactly but surfaces lockout /
/// deactivated states with actionable messages. Registered by removing Aufy's
/// endpoint from DI and mapping this one in Program.cs.
///
/// Auth scheme: "Aufy.BearerSignInCookieScheme", whose
/// AufySignInJwtBearerHandler writes the { AccessToken, ExpiresIn } body AND
/// appends the httpOnly Aufy.RefreshToken cookie (Aufy source:
/// src/Aufy.Core/AuthSchemes/AufySignInJwtBearerHandler.cs). The
/// "Aufy.BearerSignInTokenScheme" handler only writes the body — no cookie —
/// which left the refresh endpoint with nothing to read.
/// </summary>
public static class SeraGoTokenEndpoint
{
    public static IEndpointRouteBuilder MapSeraGoTokenEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/token", HandleAsync)
            .AddEndpointFilter<ValidationEndpointFilter<TokenRequest>>()
            .AllowAnonymous()
            .WithOpenApi();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] TokenRequest req,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        // Sign-in is routed to the COOKIE scheme handler, which is the one that
        // does both halves of the exchange: the { AccessToken, ExpiresIn } body
        // the frontend reads, plus the httpOnly Aufy.RefreshToken cookie that
        // POST /api/auth/token/refresh requires. The token scheme writes only
        // the body, and the refresh endpoint accepts the token ONLY as a cookie
        // — so without this the session died at the access-token expiry with a
        // 401 → refresh → 401 loop. Consequence of the switch: `refreshToken`
        // in the body is now null (the cookie carries it instead); the frontend
        // already types it nullable and stores only the access token.
        signInManager.AuthenticationScheme = "Aufy.BearerSignInCookieScheme";
        var result = await signInManager.PasswordSignInAsync(
            req.Email, req.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // The handler already wrote the token response body.
            return TypedResults.Empty;
        }

        var user = await userManager.FindByEmailAsync(req.Email);

        // No account with this email — say so and point the user at signup.
        // Identity's PasswordSignInAsync deliberately reports the same failure
        // for unknown emails, so this lookup is the only way to tell.
        // (Product decision: same existence-revealing behavior as the
        // forgot-password and resend-confirmation endpoints.)
        if (user is null)
        {
            return TypedResults.Problem(
                "No account found with this email address.",
                statusCode: StatusCodes.Status404NotFound);
        }

        // RequireConfirmedEmail = true: Identity refuses the sign-in (NotAllowed)
        // until the email is confirmed. Only revealed for accounts that actually
        // exist. 403 (not 401) so the frontend can tell this apart from a wrong
        // password and offer a resend-confirmation link.
        if (result.IsNotAllowed)
        {
            return TypedResults.Problem(
                "Please confirm your email address first. Check your inbox for the confirmation link.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        // Lockout-aware messaging. The user is known to exist at this point
        // (checked above), so the lockout state can be surfaced safely.
        if (result.IsLockedOut)
        {
            var message = user.LockoutEnd is { } le &&
                          le >= DateTimeOffset.MaxValue.AddDays(-1)
                // DeactivateAccount sets LockoutEnd = DateTimeOffset.MaxValue.
                ? "This account has been deactivated. Contact support if this looks wrong."
                : user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow
                    ? $"Too many failed sign-in attempts. Try again in about {Math.Max(1, (int)Math.Ceiling((end - DateTimeOffset.UtcNow).TotalMinutes))} minute(s)."
                    : "Too many failed sign-in attempts. Try again shortly.";

            return TypedResults.Problem(message, statusCode: StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Problem(
            "Invalid email or password.",
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
