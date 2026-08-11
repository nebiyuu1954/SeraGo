using System.Security.Claims;
using Aufy.Core;
using Aufy.Core.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SeraGo.API.Auth;
using SeraGo.Core.Domain.Entities;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Replacement for Aufy 1.0.0's <c>SignUpExternalEndpoint</c>
/// (POST /api/auth/signup/external).
///
/// The shipped endpoint only checks whether the GOOGLE login already exists —
/// not whether the account's EMAIL already exists. So a user who signed up
/// with email+password and then clicks "Continue with Google" got a generic
/// "Error creating user" (since we made UserName = email, the duplicate
/// NormalizedUserName index now trips) instead of a working flow.
///
/// This endpoint mirrors Aufy's flow for brand-new users (reusing the existing
/// <see cref="SeraGoSignUpExternalExtension"/> events for role + password +
/// name + profile) and adds a LINK branch: if an account with the Google email
/// already exists, the Google login is attached to it
/// (<see cref="UserManager{TUser}.AddLoginAsync"/>) and THAT account is signed
/// in — one request, no duplicate account.
/// </summary>
public static class SeraGoExternalSignUpEndpoint
{
    public static IEndpointRouteBuilder MapSeraGoExternalSignUpEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/signup/external", HandleAsync)
            .AddEndpointFilter<ValidationEndpointFilter<SeraGoSignUpExternalRequest>>()
            .RequireAuthorization(policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddAuthenticationSchemes("Aufy.ExternalSignUpScheme");
            })
            .WithOpenApi();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] SeraGoSignUpExternalRequest req,
        HttpContext context,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IServiceProvider serviceProvider,
        IOptions<AufyOptions> options,
        ILoggerFactory loggerFactory)
    {
        // Same as Aufy: consume the one-time OAuth cookie either way.
        await context.SignOutAsync("Aufy.ExternalSignUpScheme");

        var identity = context.User.Identity;
        if (identity is null || !identity.IsAuthenticated || identity.AuthenticationType is null)
        {
            return TypedResults.Unauthorized();
        }

        var providerKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (providerKey is null)
        {
            return TypedResults.Problem(
                "Google sign-in did not provide a user identifier. Please try again.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var provider = identity.AuthenticationType;
        var email = context.User.FindFirstValue(ClaimTypes.Email);
        var logger = loggerFactory.CreateLogger("SeraGoExternalSignUpEndpoint");

        // Already linked (e.g. a repeated attempt) — just sign in.
        var linkedUser = await userManager.FindByLoginAsync(provider, providerKey);
        if (linkedUser is not null)
        {
            return await SignInUserAsync(signInManager, linkedUser);
        }

        // A SeraGo account with this email already exists (email/password) —
        // link the Google login to it instead of failing with a duplicate
        // account. Google verifies the email during OAuth, so matching by the
        // verified claim is the standard "sign in with Google" behavior.
        if (!string.IsNullOrWhiteSpace(email))
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                // RequireConfirmedEmail gates PasswordSignInAsync only — this
                // direct sign-in would bypass it. Keep unconfirmed accounts
                // inactive: refuse to link/sign in until the email is verified.
                if (!existing.EmailConfirmed)
                {
                    return TypedResults.Problem(
                        "An account already exists for this email but hasn't been confirmed yet. " +
                        "Confirm your email first, then sign in with Google.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var linkResult = await userManager.AddLoginAsync(
                    existing, new UserLoginInfo(provider, providerKey, "Google"));
                if (!linkResult.Succeeded)
                {
                    logger.LogError("Failed to link Google login to {Email}: {Errors}",
                        email, string.Join(", ", linkResult.Errors.Select(e => e.Description)));
                    return TypedResults.Problem(
                        "Could not link your Google account. Please try again.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                logger.LogInformation("Linked Google login to existing account {Email}", email);
                return await SignInUserAsync(signInManager, existing);
            }
        }

        // Brand-new Google user: mirror Aufy's flow. UserName must be the email
        // so email+password login (lookup by UserName) can find the account.
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        // Events hook: validates role + password, hashes the password, fills in
        // the name from Google's claims, sets UserType.
        var events = serviceProvider
            .GetService<ISignUpExternalEndpointEvents<ApplicationUser, SeraGoSignUpExternalRequest>>();
        if (events is not null)
        {
            var problem = await events.UserCreatingAsync(req, context.Request, user);
            if (problem is not null)
            {
                return problem;
            }
        }

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            logger.LogError("Error creating user: {Email}. Result: {Result}", user.Email, createResult);
            return TypedResults.Problem(
                string.Join(", ", createResult.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status400BadRequest);
        }

        var addLoginResult = await userManager.AddLoginAsync(
            user, new UserLoginInfo(provider, providerKey, "Google"));
        if (!addLoginResult.Succeeded)
        {
            await userManager.DeleteAsync(user); // never leave a half-created account
            logger.LogError("Failed to add Google login to user {UserId}: {Errors}",
                user.Id, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
            return TypedResults.Problem("There was an error creating your account.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var rolesResult = await userManager.AddToRolesAsync(user, options.Value.DefaultRoles);
        if (!rolesResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return TypedResults.Problem("There was an error creating your account.", statusCode: StatusCodes.Status500InternalServerError);
        }

        if (events is not null)
        {
            // Adds the self-selected role + creates the role profile row
            // (rolls the user back on failure).
            await events.UserCreatedAsync(req, context.Request, user);
        }

        return await SignInUserAsync(signInManager, user);
    }

    /// <summary>Signs the user in via Aufy's JWT handler (writes the token pair + refresh cookie).</summary>
    private static async Task<IResult> SignInUserAsync(
        SignInManager<ApplicationUser> signInManager, ApplicationUser user)
    {
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        return TypedResults.SignIn(principal, null, "Aufy.BearerSignInCookieScheme");
    }
}
