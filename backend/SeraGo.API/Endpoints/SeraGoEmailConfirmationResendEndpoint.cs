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
/// Replacement for Aufy 1.0.0's <c>EmailConfirmationResendEndpoint</c>
/// (POST /api/account/email/confirm/resend). The shipped endpoint always
/// returns 200 with no body, so the UI can only ever say the generic
/// "If an account exists with that email…" — it can't tell the user whether
/// the account exists or whether it's already verified.
///
/// Product decision (same as forgot-password): surface the real outcome.
///   - unknown email       → 404 "No account found with this email address."
///   - already verified    → 409 "This email is already verified…"
///   - unconfirmed account → confirmation email sent, 200
/// This does allow account enumeration, which the classic always-200 contract
/// prevents — consistent with the existing forgot-password behavior.
/// </summary>
public static class SeraGoEmailConfirmationResendEndpoint
{
    public static IEndpointRouteBuilder MapSeraGoEmailConfirmationResendEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/email/confirm/resend", async (
                [FromBody] ResendConfirmationRequest req,
                [FromServices] UserManager<ApplicationUser> manager,
                [FromServices] IAufyEmailSenderManager<ApplicationUser> emailSender,
                [FromServices] IOptions<AufyOptions> options,
                [FromServices] ILoggerFactory loggerFactory,
                [FromServices] EmailThrottleService throttle,
                HttpRequest httpRequest) =>
            {
                var logger = loggerFactory.CreateLogger("SeraGo.ResendConfirmation");
                ArgumentException.ThrowIfNullOrWhiteSpace(req.Email, nameof(req.Email));

                var user = await manager.FindByEmailAsync(req.Email);
                if (user is null)
                {
                    // Explicit existence check: tell the user no account was found.
                    return Results.Problem(
                        "No account found with this email address.",
                        statusCode: StatusCodes.Status404NotFound);
                }

                if (user.EmailConfirmed)
                {
                    // Nothing to send — the email is already verified.
                    return Results.Problem(
                        "This email is already verified — you can sign in now.",
                        statusCode: StatusCodes.Status409Conflict);
                }

                // Per-email budget so one address can't drain the email sender
                // (EmailJS credits) by re-requesting confirmation links on a loop.
                if (!throttle.TryAllow(req.Email, out var retryAfterMessage))
                {
                    return Results.Problem(
                        retryAfterMessage,
                        statusCode: StatusCodes.Status429TooManyRequests);
                }

                var code = await manager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var baseUri = new Uri(
                    new Uri(options.Value.ClientApp.BaseUrl ?? $"{httpRequest.Scheme}://{httpRequest.Host}"),
                    options.Value.ClientApp.EmailConfirmationPath);
                var link = new Uri(baseUri, $"?code={code}&userId={user.Id}");
                await emailSender.SendEmailConfirmationAsync(user, link.ToString());

                logger.LogInformation("Confirmation email sent to {Email}", user.Email);
                return Results.Ok();
            })
            .WithTags("Account")
            .AllowAnonymous()
            .WithOpenApi();

        return app;
    }

    public sealed record ResendConfirmationRequest
    {
        [Required, EmailAddress]
        public string? Email { get; set; }
    }
}
