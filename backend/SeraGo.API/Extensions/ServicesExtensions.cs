using FluentEmail.Core.Interfaces;
using FluentEmail.SendGrid;
using Microsoft.AspNetCore.Identity;
using SeraGo.API.Auth;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Hangfire;
using Hangfire.PostgreSql;

using Telegram.Bot;
using SeraGo.API.Email;
using SeraGo.API.Services;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using StackExchange.Redis;
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
    public static IServiceCollection SetupIdentity(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Surface Google OAuth gaps in Production: with empty ClientId/Secret the
        // Google provider is silently not registered, /api/auth/external/providers
        // reports nothing, and the frontend's Google button just stays disabled —
        // no error anywhere. Warn (do NOT fail: email/password auth still works).
        if (environment.IsProduction()
            && (string.IsNullOrWhiteSpace(configuration["Aufy:Providers:Google:ClientId"])
                || string.IsNullOrWhiteSpace(configuration["Aufy:Providers:Google:ClientSecret"])))
        {
            Console.WriteLine("[config] WARNING: Google OAuth not configured — Google sign-in is unavailable. "
                + "Set Aufy__Providers__Google__ClientId and ClientSecret.");
        }

        services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        var jwtKey = configuration["Aufy:JwtBearer:SigningKey"];
        if (!string.IsNullOrEmpty(jwtKey))
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(jwtKey);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddGoogle(options =>
            {
                options.ClientId = configuration["Aufy:Providers:Google:ClientId"] ?? "";
                options.ClientSecret = configuration["Aufy:Providers:Google:ClientSecret"] ?? "";
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
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
        // sending them. Gated to the Development environment — previously this was
        // enabled by mere presence of "FluentEmail:SaveEmailsOnDisk" in the BASE
        // appsettings.json, so a production deploy with no email provider silently
        // wrote confirmation emails to a local folder instead of sending them,
        // leaving every new user unable to confirm their account and sign in.
        var emailDir = configuration["FluentEmail:SaveEmailsOnDisk"];
        if (!string.IsNullOrWhiteSpace(emailDir) && environment.IsDevelopment())
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

    // ══════════════════════════════════════════════════════════════════
    //  Notification Infrastructure: Hangfire + Redis + SignalR
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers Hangfire (background jobs using PostgreSQL), Redis
    /// (unread counts + online presence), SignalR (real-time push),
    /// and the NotificationService.
    /// </summary>
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ── Hangfire ──
        // HANGFIRE_ENABLED=false is an EMERGENCY off-switch (cost spikes), not a
        // normal setting: with it off, no storage/server/dashboard is registered,
        // email and Telegram delivery is disabled and not retried, recurring
        // jobs never register — but in-app notifications still work (they are
        // written to Postgres before the enqueue attempt). Default: on.
        if (HangfireEnabled)
        {
            // Hangfire keeps its OWN Npgsql pool, separate from EF Core's (both
            // derive from DefaultConnection but share nothing) — cap it explicitly
            // so the pools can't jointly overrun Neon's ~20-connection free-tier
            // ceiling. There is no pool-size knob on PostgreSqlStorageOptions; the
            // cap lives on the connection string Hangfire's connections are drawn
            // from. Budget: 6 covers the 4 workers (each holds a dedicated
            // connection while executing) plus schedulers/dispatchers, and
            // completes the pool budget EF 12 + Hangfire 6 + SignalR 2 = 20.
            var hangfireCs = new Npgsql.NpgsqlConnectionStringBuilder(
                configuration.GetConnectionString("DefaultConnection"));
            if (!hangfireCs.ContainsKey("Max Pool Size"))
            {
                hangfireCs.MaxPoolSize = HangfireMaxPoolSize;
            }

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    bootstrapper => bootstrapper.UseNpgsqlConnection(hangfireCs.ConnectionString),
                    new PostgreSqlStorageOptions
                    {
                        // QueuePollInterval: default is 15s; 30s halves Neon
                        // wakeups and lets the serverless DB auto-suspend overnight.
                        QueuePollInterval = TimeSpan.FromSeconds(30),
                    }));

            services.AddHangfireServer(options =>
            {
                options.Queues = ["email", "telegram", "maintenance"];
                // Fixed small count instead of ProcessorCount * 2: these queues
                // carry emails/telegram sends/maintenance — light workloads where
                // extra workers only add DB connections and Neon wakeups. Must
                // stay below Hangfire's Max Pool Size cap above.
                options.WorkerCount = Math.Min(Environment.ProcessorCount, 4);
            });
        }
        else
        {
            Console.WriteLine("[config] WARNING: HANGFIRE_ENABLED=false — background jobs are DISABLED. "
                + "In-app notifications still work; email and Telegram delivery is disabled and not retried. "
                + "Recurring jobs are skipped.");
        }

        // ── Redis ──
        // Required in Production (enforced by Program.cs startup validation):
        // without it, unread counts degrade to per-page Postgres COUNT queries
        // and SignalR presence tracking is disabled. Optional in Development —
        // NotificationService/NotificationHub take IConnectionMultiplexer? and
        // fall back gracefully when it's absent.
        var redisConnectionString = configuration["REDIS_CONNECTION"]
            ?? configuration["ConnectionStrings:Redis"];

        // ── SignalR ──
        var signalR = services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.CamelCase;
            });

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // abortConnect=false: with StackExchange.Redis's default
            // (abortConnect=true) a brief Redis blip at startup — Upstash TLS
            // handshake delays during a Render deploy, for instance — throws out
            // of Connect and the app can't boot, restart-looping on the host.
            // With it, the multiplexer starts disconnected and reconnects in the
            // background, which is what you want for a managed Redis dependency.
            // Command failures still surface normally once connected.
            var connectString = redisConnectionString.Contains("abortConnect", StringComparison.OrdinalIgnoreCase)
                ? redisConnectionString
                : redisConnectionString + ",abortConnect=false";

            // ONE multiplexer, shared: NotificationService/NotificationHub (via
            // IConnectionMultiplexer) and the SignalR backplane. This package
            // version has no IConnectionMultiplexer overload for the backplane —
            // sharing is done via RedisOptions.ConnectionFactory below. Passing
            // the raw string there would make the backplane open its own second
            // connection — wasteful, and connection count is limited on
            // Upstash's free tier. Connect eagerly here (not inside a sp =>
            // lambda) so a bad Redis URL fails at DI registration, keeping the
            // fail-fast policy from the startup validation block. With
            // abortConnect=false above, a well-formed but UNREACHABLE host still
            // boots (multiplexer retries in the background) — only config
            // errors (malformed URL, auth rejected) fail fast.
            StackExchange.Redis.IConnectionMultiplexer multiplexer;
            try
            {
                multiplexer = ConnectionMultiplexer.Connect(connectString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "REDIS_CONNECTION could not be connected — fix the value (check host, port, and password) " +
                    "or unset it to run without Redis. Underlying error: " + ex.Message, ex);
            }
            services.AddSingleton(multiplexer);
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);

            // Register for disposal with the host so a clean shutdown closes
            // the Redis connection instead of leaking it until process exit.
            // (The backplane's lifetime manager may also dispose it on shutdown;
            // IConnectionMultiplexer.Dispose is idempotent, so that's benign.)
            services.AddSingleton<IHostedService>(_ => new DisposerHostedService(multiplexer));

            // Redis backplane: adds one subscription per hub method group —
            // effectively free for a single instance, and the day this API
            // scales to 2+ instances, pushes emitted by instance B reach
            // clients connected to instance A (without it, only same-instance
            // clients get real-time events).
            //
            // Share the ONE multiplexer above via ConnectionFactory — this
            // package version has no IConnectionMultiplexer overload, and the
            // string overloads would make the backplane open a second
            // connection. With ConnectionFactory set, the backplane uses the
            // returned multiplexer as-is (no own connect, no extra socket).
            // Channels are namespaced automatically by hub type
            // ("SeraGo.API.Hubs.NotificationHub:all", ":group:*", …), so no
            // explicit ChannelPrefix is needed.
            signalR.AddStackExchangeRedis(options =>
            {
                options.ConnectionFactory = _ => Task.FromResult<IConnectionMultiplexer>(multiplexer);
            });
        }

        // ── Notification service ──
        services.AddScoped<NotificationService>();
        services.AddScoped<NotificationOrchestrator>();

        // ── Telegram Bot ──
        // Only registered when TELEGRAM_BOT_TOKEN is set.
        // When absent, Telegram endpoints are not mapped (see Program.cs).
        var telegramToken = configuration["TELEGRAM_BOT_TOKEN"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

        if (!string.IsNullOrWhiteSpace(telegramToken))
        {
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramToken));
            services.AddScoped<TelegramBotService>();
        }

        // ── Authorization policies ──
        // Roles come from ASP.NET Core Identity (added by Aufy via
        // AddIdentityCore<TUser>().AddRoles<IdentityRole>() in SetupAufy).
        // `[Authorize(Roles = "Admin")]` / `[Authorize(Roles = Roles.Admin)]`
        // resolve to a policy named "Admin" / "Talent" at
        // runtime, so those policy names must exist in the container.
        services.AddAuthorization(options =>
        {
            foreach (var role in new[] { "Admin", "Talent" })
            {
                options.AddPolicy(role, policy => policy.RequireRole(role));
            }

            // Internal API: shared secret (X-Api-Key header) for Django → .NET
            // batch notifications. Not a user role.
            options.AddPolicy("InternalApi", policy =>
            {
                policy.AddAuthenticationSchemes("ApiKey");
                policy.RequireAuthenticatedUser();
            });
        });

        services.AddAuthentication()
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyHandler>(
                "ApiKey", options => { });

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

    /// <summary>
    /// Hangfire's pool cap (must cover WorkerCount + schedulers/dispatchers).
    /// Budgeted so EF Core (12) + Hangfire (6) + SignalR backplane (2) = 20,
    /// Neon's free-tier connection ceiling — see .env.example.
    /// </summary>
    public const int HangfireMaxPoolSize = 6;

    /// <summary>
    /// SignalR backplane connection budget: one pub/sub + one interactive
    /// connection for the shared multiplexer (0 when Redis is unconfigured).
    /// </summary>
    public const int SignalRBackplaneConnections = 2;

    /// <summary>
    /// Emergency kill switch for background jobs (HANGFIRE_ENABLED env var).
    /// Read in ONE place so Program.cs (dashboard, recurring-job registration)
    /// and DI (storage, server) can never disagree.
    /// Env-var-only by design: unlike config-bound switches this one must stay
    /// readable even when configuration providers themselves are the problem
    /// (and it matches how AdminApiOptions reads its flag).
    /// </summary>
    public static bool HangfireEnabled =>
        Environment.GetEnvironmentVariable("HANGFIRE_ENABLED") is not { } raw
        || raw.Trim().ToLowerInvariant() is not ("0" or "false" or "no" or "off");

    /// <summary>
    /// Stops the shared Redis <see cref="IConnectionMultiplexer"/> when the host
    /// shuts down. Multiplexer.DisposeAsync also flushes pending commands, so
    /// wiring it into the host lifetime is preferable to waiting for process
    /// teardown.
    /// </summary>
    private sealed class DisposerHostedService(IConnectionMultiplexer multiplexer) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await multiplexer.DisposeAsync();
            }
            catch
            {
                // Shutdown-time cleanup must never mask the real stop reason.
            }
        }
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
    ///
    /// Auth endpoints additionally go through a GLOBAL partitioned limiter
    /// (auth_signin / auth_signup / auth_email) instead of per-endpoint
    /// policies: several of those routes (e.g. /api/auth/signin,
    /// /api/auth/token/refresh) are registered inside the Aufy library and
    /// can't be annotated with .RequireRateLimiting(...). A global limiter with
    /// a (method, path) matcher covers them all; everything else gets
    /// NoLimiter and behaves exactly as before. auth_email is per-IP by
    /// design — per-email throttling is EmailThrottleService's job.
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

            // ── Auth endpoint limits (global partitioned limiter) ──────────
            // Runs for every request; non-auth traffic gets NoLimiter. The
            // global limiter and the OnRejected above compose: rejected auth
            // requests produce the same 429 envelope as job-limit rejections.
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var path = httpContext.Request.Path;
                var isPost = HttpMethods.IsPost(httpContext.Request.Method);

                // Same fixed-window shape as the named policies, sharing the
                // X-Forwarded-For-aware PartitionKey helper. The key is prefixed
                // with the policy name — the global limiter caches by TKey, so
                // all three policies sharing a bare IP would collide into ONE
                // bucket (whichever was created first wins for every policy).
                RateLimitPartition<string> AuthPartition(string policy, RateLimitPolicyOptions o) =>
                    RateLimitPartition.GetFixedWindowLimiter<string>(
                        $"{policy}:{PartitionKey(httpContext)}",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = o.PermitLimit,
                            Window = TimeSpan.FromMinutes(o.WindowMinutes),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        });

                // Login + refresh — password spraying and refresh-token probing.
                // StartsWithSegments intentionally sweeps /api/auth/token/refresh
                // and /api/auth/signin/refresh into the same bucket.
                if (isPost && (path.StartsWithSegments("/api/auth/token")
                            || path.StartsWithSegments("/api/auth/signin")))
                {
                    return AuthPartition("signin", options.AuthSignin);
                }

                // Signup — bulk fake-account creation and email-credit burn.
                if (isPost && path.StartsWithSegments("/api/auth/signup"))
                {
                    return AuthPartition("signup", options.AuthSignup);
                }

                // Email-bearing flows — forgot password + resend confirmation
                // (email bombing). Deliberately per-IP: EmailThrottleService
                // already caps sends per recipient address; this limiter stops
                // the request flood (CPU/DB/token generation) before that.
                // NOTE: the confirm LINK (GET /api/account/email/confirm) is
                // intentionally not limited — real users click it once per
                // email and shared-IP offices would collide here.
                if (isPost && (path.StartsWithSegments("/api/account/password/forgot")
                            || path.StartsWithSegments("/api/account/email/confirm/resend")))
                {
                    return AuthPartition("email", options.AuthEmail);
                }

                return RateLimitPartition.GetNoLimiter<string>("no-limit");
            });
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

    /// <summary>POST /api/auth/token*, /api/auth/signin* — login + refresh. Default: 10 / 15 min.</summary>
    public RateLimitPolicyOptions AuthSignin { get; set; } = new() { PermitLimit = 10, WindowMinutes = 15 };

    /// <summary>POST /api/auth/signup* — signup + external signup. Default: 5 / hour.</summary>
    public RateLimitPolicyOptions AuthSignup { get; set; } = new() { PermitLimit = 5, WindowMinutes = 60 };

    /// <summary>POST forgot-password + resend-confirmation. Per-IP. Default: 5 / hour.</summary>
    public RateLimitPolicyOptions AuthEmail { get; set; } = new() { PermitLimit = 5, WindowMinutes = 60 };
}

/// <summary>Fixed-window policy settings for one endpoint group.</summary>
public sealed class RateLimitPolicyOptions
{
    /// <summary>Max requests per window (defaults apply when the config key is absent).</summary>
    public int PermitLimit { get; set; } = 60;

    /// <summary>Window length in minutes.</summary>
    public int WindowMinutes { get; set; } = 1;
}
