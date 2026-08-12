using Aufy.Core;
using Aufy.Core.AuthSchemes;
using Aufy.Core.Endpoints;
using Aufy.EntityFrameworkCore;
using Aufy.FluentEmail;
using FluentEmail.Core.Interfaces;
using FluentEmail.SendGrid;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SeraGo.API.Auth;
using SeraGo.API.Email;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using System.Threading.RateLimiting;

namespace SeraGo.API.Extensions;

public static class ServicesExtensions
{
    /// <summary>
    /// Wires up Aufy (ASP.NET Core Identity + JWT bearer) with the custom
    /// signup model (profile fields + role selection) and Google as an
    /// external (OAuth) sign-in provider.
    ///
    /// Google is only registered when credentials exist under
    /// `Aufy:Providers:Google` (see <see cref="AufyOptions.Providers"/>) — with
    /// blank ClientId/ClientSecret the provider stays disabled and the app
    /// behaves as before.
    /// </summary>
    public static IServiceCollection SetupAufy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAufy<ApplicationUser>(configuration)
            .UseSignUpModel<SeraGoSignUpRequest>()
            // Custom external signup: new Google users must pick a role and
            // display name once, then POST /api/auth/signup/external.
            .UseExternalSignUpModel<SeraGoSignUpExternalRequest>()
            .AddProvider("Google", (auth, options) =>
            {
                auth.AddGoogle(o => o.Configure("Google", options));
            })
            .AddEntityFrameworkStore<ApplicationDbContext, ApplicationUser>()
            .AddFluentEmail(registerMailKitSender: false);

        services.AddScoped<ISignUpEndpointEvents<ApplicationUser, SeraGoSignUpRequest>, SeraGoSignUpExtension>();
        services.AddScoped<ISignUpExternalEndpointEvents<ApplicationUser, SeraGoSignUpExternalRequest>, SeraGoSignUpExternalExtension>();

        // Aufy 1.0.0's RefreshTokenStore has a bug: SaveAsync passes the
        // CancellationToken as a second key to FindAsync (the AufyRefreshTokens
        // PK is UserId alone), so the existing row is never found and every
        // sign-in INSERTs a duplicate -> UNIQUE constraint failure on the
        // second login. Swap in a corrected store that rotates the token in
        // place instead (see SeraGoRefreshTokenStore). RemoveAll is defensive:
        // ours is registered last either way, so it wins constructor injection.
        services.RemoveAll<IRefreshTokenStore>();
        services.AddScoped<IRefreshTokenStore, SeraGoRefreshTokenStore>();

        // Aufy 1.0.0's PasswordForgotEndpoint 500s on unknown emails (missing
        // null guard before GeneratePasswordResetTokenAsync). Remove it from DI
        // so MapAufyEndpoints skips it; the fixed replacement is mapped in
        // Program.cs (ForgotPasswordEndpoint.cs).
        var forgotDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IAccountEndpoint) &&
            d.ImplementationType == typeof(PasswordForgotEndpoint<ApplicationUser>));
        if (forgotDescriptor is not null)
        {
            services.Remove(forgotDescriptor);
        }

        // Replace Aufy's TokenEndpoint: the shipped one returns the same
        // "Invalid email or password" for lockouts too. Our replacement
        // (SeraGoTokenEndpoint) keeps the identical sign-in flow but explains
        // lockout / deactivated states. Remove from DI so MapAufyEndpoints
        // skips it; the replacement is mapped in Program.cs. Matched on the
        // open generic so the removal can't silently miss if the closed type
        // ever changes (a miss would map both endpoints -> ambiguous route).
        var tokenDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IAuthEndpoint) &&
            d.ImplementationType?.GetGenericTypeDefinition() == typeof(TokenEndpoint<>));
        if (tokenDescriptor is not null)
        {
            services.Remove(tokenDescriptor);
        }

        // Replace Aufy's SignUpExternalEndpoint: the shipped one doesn't check
        // whether the account's EMAIL already exists, so "Continue with Google"
        // on an email/password account errors with a duplicate-account failure.
        // Our replacement (SeraGoExternalSignUpEndpoint) links the Google login
        // to the existing account and signs it in. Remove from DI so
        // MapAufyEndpoints skips it; the replacement is mapped in Program.cs.
        var externalSignUpDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IAuthEndpoint) &&
            d.ImplementationType?.GetGenericTypeDefinition() == typeof(SignUpExternalEndpoint<,>));
        if (externalSignUpDescriptor is not null)
        {
            services.Remove(externalSignUpDescriptor);
        }

        // Replace Aufy's WhoAmIEndpoint: the shipped one builds its response
        // from JWT claims only, so it can't report emailConfirmed — needed to
        // keep unconfirmed users out of the dashboards. Our replacement
        // (SeraGoWhoAmIEndpoint) loads the user row and adds that flag.
        var whoAmIDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IAuthEndpoint) &&
            d.ImplementationType?.GetGenericTypeDefinition() == typeof(WhoAmIEndpoint<>));
        if (whoAmIDescriptor is not null)
        {
            services.Remove(whoAmIDescriptor);
        }

        // Replace Aufy's EmailConfirmationResendEndpoint: the shipped one
        // always returns 200 with no body, so the UI can't tell users whether
        // the account exists or is already verified. Our replacement
        // (SeraGoEmailConfirmationResendEndpoint) surfaces 404/409/200.
        var resendDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IAccountEndpoint) &&
            d.ImplementationType?.GetGenericTypeDefinition() == typeof(EmailConfirmationResendEndpoint<>));
        if (resendDescriptor is not null)
        {
            services.Remove(resendDescriptor);
        }

        // Replace Aufy's EmailConfirmEndpoint: the shipped one returns 404 for
        // ALREADY-confirmed emails too, so re-clicking a confirmation link
        // shows the same misleading "invalid or expired" screen as a bad code.
        // Our replacement (SeraGoEmailConfirmEndpoint) treats already-confirmed
        // as an idempotent 200 success.
        var confirmDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IAccountEndpoint) &&
            d.ImplementationType?.GetGenericTypeDefinition() == typeof(EmailConfirmEndpoint<>));
        if (confirmDescriptor is not null)
        {
            services.Remove(confirmDescriptor);
        }

        // CORS for the frontend. Always registers a policy so app.UseCors() can never
        // fail at startup, even if Aufy:ClientApp:BaseUrl is missing.
        var clientAppUrl = configuration["Aufy:ClientApp:BaseUrl"];
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (!string.IsNullOrWhiteSpace(clientAppUrl))
                {
                    policy.WithOrigins(clientAppUrl)
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    // No explicit origin configured: allow any origin but no credentials.
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
            });
        });

        // FluentEmail.Core DI (email factory + default NullSender).
        services.AddFluentEmail(
            configuration["FluentEmail:FromEmail"] ?? "noreply@serago.local",
            configuration["FluentEmail:FromName"] ?? "SeraGo");

        // Dev-only: write emails to disk (see SeraGo.API/logs/emails) instead of
        // sending them. Keep "FluentEmail:SaveEmailsOnDisk" for local development.
        var emailDir = configuration["FluentEmail:SaveEmailsOnDisk"];
        if (!string.IsNullOrWhiteSpace(emailDir))
        {
            Directory.CreateDirectory(emailDir);
            services.Replace(ServiceDescriptor.Scoped<ISender>(_ => new SaveToDiskSender(emailDir)));
        }

        // Real email delivery: when a SendGrid API key is present
        // (SENDGRID_API_KEY env var, or FluentEmail:SendGridApiKey config),
        // replace the dev disk sender with SendGrid. The env var is preferred
        // first: appsettings.json carries an EMPTY SendGridApiKey, so it must
        // never shadow a real key from the environment. Note: the "from"
        // address must be a verified sender in the SendGrid account (domain
        // auth or single sender) or sends are rejected.
        var sendGridApiKey = ReadFirstNonEmpty(configuration, "SENDGRID_API_KEY", "FluentEmail:SendGridApiKey");
        if (!string.IsNullOrWhiteSpace(sendGridApiKey))
        {
            // RemoveAll is defensive: no other ISender registration (disk
            // sender, NullSender, ...) can silently win over SendGrid.
            services.RemoveAll<ISender>();
            services.AddScoped<ISender>(_ => new SendGridSender(sendGridApiKey));
        }

        // EmailJS relay — for regions where classic providers are unreachable
        // (e.g. Ethiopia). When EmailJs:ServiceId/TemplateId/PublicKey are
        // configured (or their env vars), every email goes through EmailJS's
        // cloud REST API instead. Registered LAST so it wins over SendGrid/disk
        // when both are set. See EmailJsSender for the one-time template setup.
        var emailJsServiceId = ReadFirstNonEmpty(configuration, "EMAILJS_SERVICE_ID", "EmailJs:ServiceId");
        var emailJsTemplateId = ReadFirstNonEmpty(configuration, "EMAILJS_TEMPLATE_ID", "EmailJs:TemplateId");
        var emailJsPublicKey = ReadFirstNonEmpty(configuration, "EMAILJS_PUBLIC_KEY", "EmailJs:PublicKey");
        if (!string.IsNullOrWhiteSpace(emailJsServiceId)
            && !string.IsNullOrWhiteSpace(emailJsTemplateId)
            && !string.IsNullOrWhiteSpace(emailJsPublicKey))
        {
            var emailJsOptions = new EmailJsSenderOptions
            {
                ServiceId = emailJsServiceId,
                TemplateId = emailJsTemplateId,
                PublicKey = emailJsPublicKey,
                PrivateKey = ReadFirstNonEmpty(configuration, "EMAILJS_PRIVATE_KEY", "EmailJs:PrivateKey") ?? string.Empty,
                FromName = configuration["FluentEmail:FromName"] ?? "SeraGo",
                FromEmail = configuration["FluentEmail:FromEmail"] ?? string.Empty,
            };
            services.RemoveAll<ISender>();
            services.AddScoped<ISender>(sp => new EmailJsSender(
                emailJsOptions,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<EmailJsSender>>()));
        }

        return services;
    }

    /// <summary>Returns the first configured (non-empty) value among the keys.</summary>
    private static string? ReadFirstNonEmpty(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }

    /// <summary>Swagger/OpenAPI with JWT Bearer support for testing protected endpoints.</summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SeraGo API",
                Version = "v1",
                Description = "SeraGo job board backend. Get a token from POST /api/auth/token, then paste it into Authorize.",
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the access_token returned by POST /api/auth/token",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    },
                    Array.Empty<string>()
                },
            });
        });

        return services;
    }

    /// <summary>
    /// API rate limiting (throttling). Policies are keyed per remote IP with a
    /// fixed window; read and write traffic get their own limits. Endpoints opt
    /// in via <c>.RequireRateLimiting("jobs_read")</c> / "jobs_write" (see
    /// JobEndpoints). Limits come from the "RateLimiting" config section.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("RateLimiting").Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Too many requests — please slow down and try again shortly." },
                    cancellationToken);
            };

            limiter.AddPolicy("jobs_read", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.JobsRead.PermitLimit,
                    Window = TimeSpan.FromMinutes(options.JobsRead.WindowMinutes),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));

            limiter.AddPolicy("jobs_write", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.JobsWrite.PermitLimit,
                    Window = TimeSpan.FromMinutes(options.JobsWrite.WindowMinutes),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        });

        return services;
    }

    /// <summary>
    /// Buckets clients by their real IP: the first X-Forwarded-For hop when
    /// present (reverse proxies), otherwise the connection's remote address.
    /// Without this, every client behind a proxy would share one bucket.
    /// </summary>
    private static string PartitionKey(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>Config-bound options for <c>AddRateLimiting</c> ("RateLimiting" section).</summary>
public sealed class RateLimitOptions
{
    public RateLimitPolicyOptions JobsRead { get; set; } = new();
    public RateLimitPolicyOptions JobsWrite { get; set; } = new();
}

/// <summary>Fixed-window policy settings for one endpoint group.</summary>
public sealed class RateLimitPolicyOptions
{
    /// <summary>Max requests per window (defaults apply when the config key is absent).</summary>
    public int PermitLimit { get; set; } = 60;

    /// <summary>Window length in minutes.</summary>
    public int WindowMinutes { get; set; } = 1;
}
