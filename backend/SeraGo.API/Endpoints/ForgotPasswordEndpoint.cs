using System.ComponentModel.DataAnnotations;
using System.Text;
using Aufy.Core;
using Aufy.Core.EmailSender;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SeraGo.API.Email;
using SeraGo.Core.Domain.Entities;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Drop-in replacement for Aufy 1.0.0's <c>PasswordForgotEndpoint</c>, which
/// crashes with <see cref="ArgumentNullException"/> when the email is unknown:
/// with <c>RequireConfirmedEmail = false</c> its only early return is skipped,
/// so it reaches <c>GeneratePasswordResetTokenAsync(null)</c> and returns 500 —
/// which also leaks account existence.
///
/// Product decision: unknown emails get an explicit 404 "No account found"
/// instead of a fake "check your inbox" — so users immediately know the email
/// isn't registered (this does allow account enumeration, which the classic
/// always-200 contract prevents). Existing accounts receive the reset email.
/// </summary>
public static class ForgotPasswordEndpoint
{
    public static IEndpointRouteBuilder MapForgotPasswordEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/password/forgot", async (
                [FromBody] PasswordForgotRequest req,
                [FromServices] UserManager<ApplicationUser> manager,
                [FromServices] IAufyEmailSenderManager<ApplicationUser> emailSender,
                [FromServices] IOptions<IdentityOptions> identityOptions,
                [FromServices] IOptions<AufyOptions> options,
                [FromServices] ILoggerFactory loggerFactory,
                [FromServices] EmailThrottleService throttle,
                HttpRequest httpRequest) =>
            {
                var logger = loggerFactory.CreateLogger("SeraGo.ForgotPassword");
                ArgumentException.ThrowIfNullOrWhiteSpace(req.Email, nameof(req.Email));

                var user = await manager.FindByEmailAsync(req.Email);
                if (user is null)
                {
                    // Explicit existence check: tell the user no account was found.
                    return Results.Problem(
                        "No account found with this email address.",
                        statusCode: StatusCodes.Status404NotFound);
                }

                if (identityOptions.Value.SignIn.RequireConfirmedEmail && user is not { EmailConfirmed: true })
                {
                    // Don't leak whether the account exists.
                    return Results.Ok();
                }

                // Per-email budget so one address can't drain the email sender
                // (EmailJS credits) by re-requesting reset links on a loop.
                if (!throttle.TryAllow(req.Email, out var retryAfterMessage))
                {
                    return Results.Problem(
                        retryAfterMessage,
                        statusCode: StatusCodes.Status429TooManyRequests);
                }

                var code = await manager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var baseUri = new Uri(
                    new Uri(options.Value.ClientApp.BaseUrl ?? $"{httpRequest.Scheme}://{httpRequest.Host}"),
                    options.Value.ClientApp.PasswordResetPath);
                var link = new Uri(baseUri, $"?code={code}&email={Uri.EscapeDataString(user.Email)}");
                // Note: Aufy 1.0.0 names this SendPasswordForgotAsync (renamed
                // to SendPasswordResetAsync on GitHub main).
                await emailSender.SendPasswordForgotAsync(user, link.ToString());

                logger.LogInformation("Password reset email sent to {Email}", user.Email);
                return Results.Ok();
            })
            .WithTags("Account")
            .AllowAnonymous()
            .WithOpenApi();

        return app;
    }

    public sealed record PasswordForgotRequest
    {
        [Required, EmailAddress]
        public string? Email { get; set; }
    }
}
