using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SeraGo.API.Auth;

/// <summary>
/// Simple API key authentication handler for internal endpoints.
/// The batch notification endpoint uses this so Django can call it
/// without a JWT token — just a shared API key in the X-API-Key header.
/// </summary>
public class ApiKeyHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for API key in header
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Validate against configured key
        var configuredApiKey = Environment.GetEnvironmentVariable("INTERNAL_API_KEY");

        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            Logger.LogWarning("INTERNAL_API_KEY not configured — internal API auth disabled");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!string.Equals(providedApiKey, configuredApiKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Create a claims identity for the internal service
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "internal-service"),
            new Claim(ClaimTypes.Name, "Internal API"),
            new Claim("auth_type", "api_key")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
