using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SeraGo.Core.Domain.Entities;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Replacement for Aufy 1.0.0's <c>EmailConfirmEndpoint</c>
/// (GET /api/account/email/confirm?code=…&amp;userId=…).
///
/// The shipped endpoint returns 404 when the account's email is ALREADY
/// confirmed, so clicking a link for a confirmed account shows the same
/// "invalid or expired" screen as a genuinely bad link — misleading, since
/// re-clicking a confirmation link is a no-op success, not a failure.
///
/// Behavior:
///   - unknown userId → 404 (no account)
///   - already confirmed → 200 (idempotent success — the goal is achieved)
///   - bad/expired code → 404
/// </summary>
public static class SeraGoEmailConfirmEndpoint
{
    public static IEndpointRouteBuilder MapSeraGoEmailConfirmEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/account/email/confirm", async (
                [FromQuery] string code,
                [FromQuery] string userId,
                [FromServices] UserManager<ApplicationUser> manager,
                [FromServices] ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("SeraGo.EmailConfirm");
                ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
                ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

                var user = await manager.FindByIdAsync(userId);
                if (user is null)
                {
                    return Results.NotFound();
                }

                // Already verified — clicking the link again is a success
                // (e.g. the email was confirmed on another device). Aufy's
                // endpoint 404s here, which the UI misreads as "invalid".
                if (user.EmailConfirmed)
                {
                    return Results.Ok();
                }

                var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await manager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    logger.LogInformation(
                        "Error confirming email for user with ID {UserId}. Result: {Result}",
                        user.Id, result);
                    return Results.NotFound();
                }

                logger.LogInformation("User: {UserId} confirmed email successfully", user.Id);
                return Results.Ok();
            })
            .WithTags("Account")
            .AllowAnonymous()
            .WithOpenApi();

        return app;
    }
}
