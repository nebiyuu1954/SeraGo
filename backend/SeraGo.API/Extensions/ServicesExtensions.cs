using Aufy.Core;
using Aufy.Core.AuthSchemes;
using Aufy.Core.Endpoints;
using Aufy.EntityFrameworkCore;
using Aufy.FluentEmail;
using FluentEmail.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using SeraGo.API.Auth;
using SeraGo.API.Email;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;

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

        // Dev-only: write emails to disk (see SeraGo.API/logs/emails) instead of SMTP.
        // Remove "FluentEmail:SaveEmailsOnDisk" and configure FluentEmail:Smtp* to send for real.
        var emailDir = configuration["FluentEmail:SaveEmailsOnDisk"];
        if (!string.IsNullOrWhiteSpace(emailDir))
        {
            Directory.CreateDirectory(emailDir);
            services.Replace(ServiceDescriptor.Scoped<ISender>(_ => new SaveToDiskSender(emailDir)));
        }

        return services;
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
}
