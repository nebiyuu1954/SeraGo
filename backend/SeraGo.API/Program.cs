
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Email;
using SeraGo.API.Endpoints;
using SeraGo.API.Extensions;
using SeraGo.API.Hubs;
using SeraGo.API.Middleware;
using SeraGo.API.Services;
using SeraGo.Infrastructure;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

// Load .env before configuration is built — .NET has no native .env support.
// Searches up from BOTH the working directory and the assembly location
// (bin/Debug/net8.0), plus the repo-root backend/SeraGo.API folder for every
// dir on those chains. That covers every launch style: `dotnet run` from
// backend/SeraGo.API, `dotnet run --project` from the repo root, an IDE, or
// the exe launched directly with any working directory. Existing process env
// vars win over .env (NoClobber), so real secrets always take precedence. The
// scraper DB creds live in .env(SeraGo-Scraper), loaded the same way.
var envDirs = new List<string>();
foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
{
    for (var envDir = new DirectoryInfo(start); envDir is not null; envDir = envDir.Parent)
    {
        envDirs.Add(envDir.FullName);
        var apiDir = Path.Combine(envDir.FullName, "backend", "SeraGo.API");
        if (Directory.Exists(apiDir))
        {
            envDirs.Add(apiDir);
        }
    }
}

foreach (var envFile in new[] { ".env", ".env(SeraGo-Scraper)", ".env(SeraGo-AI)" })
{
    foreach (var dir in envDirs)
    {
        var envPath = Path.Combine(dir, envFile);
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath, DotNetEnv.Env.NoClobber());
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// ── Startup configuration validation (before the container is built) ────────
// The JWT signing key must come from the environment (Aufy__JwtBearer__SigningKey
// in .env — it is deliberately NOT committed to appsettings.json) and must be
// ≥ 32 bytes: a weak/known key lets anyone forge tokens for any user, including
// admins. Fail fast instead of booting with a broken security posture, and warn
// on other misconfigurations that would silently degrade production behavior.
{
    var signingKey = builder.Configuration["Aufy:JwtBearer:SigningKey"];

    if (string.IsNullOrWhiteSpace(signingKey))
    {
        Console.WriteLine("[config] CRITICAL: JWT signing key is not configured. "
            + "Set Aufy__JwtBearer__SigningKey in .env (≥ 32 random bytes, e.g. `openssl rand -base64 48`). See .env.example.");
        throw new InvalidOperationException(
            "Missing JWT signing key (Aufy:JwtBearer:SigningKey). See .env.example.");
    }

    var signingKeyBytes = System.Text.Encoding.UTF8.GetByteCount(signingKey);
    if (signingKeyBytes < 32)
    {
        Console.WriteLine($"[config] CRITICAL: JWT signing key is too short ({signingKeyBytes} bytes; minimum 32 = 256 bits for HMAC-SHA256). "
            + "Generate a new one with `openssl rand -base64 48`.");
        throw new InvalidOperationException(
            "JWT signing key must be at least 32 bytes (256 bits).");
    }

    // The old key was committed to the repo, so treat its distinctive markers
    // as poisoned even though it satisfies the length check.
    if (signingKey.Contains("dev-only", StringComparison.OrdinalIgnoreCase)
        || signingKey.Contains("change-me", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("[config] CRITICAL: JWT signing key looks like the previously committed dev key — "
            + "it must be considered compromised. Generate a fresh random key and set Aufy__JwtBearer__SigningKey.");
        throw new InvalidOperationException(
            "JWT signing key matches the known compromised dev key. Replace it.");
    }

    if (builder.Environment.IsProduction())
    {
        // Redis is REQUIRED in Production: without it the unread-count bell
        // falls back to a Postgres COUNT per page load and SignalR presence
        // tracking is disabled — a silent degradation that only shows up as
        // DB load at scale. Fail fast rather than boot half-configured.
        // (Development stays optional — the null-multiplexer fallbacks apply.)
        if (string.IsNullOrWhiteSpace(builder.Configuration["REDIS_CONNECTION"]))
        {
            Console.WriteLine("[config] CRITICAL: Production requires REDIS_CONNECTION — unread counts "
                + "fall back to per-page Postgres queries without it, and SignalR presence tracking is disabled. "
                + "Get a free Upstash instance at https://upstash.com.");
            throw new InvalidOperationException(
                "Production requires REDIS_CONNECTION — unread counts fall back to per-page Postgres queries "
                + "without it, and SignalR presence tracking is disabled. Get a free Upstash instance at https://upstash.com.");
        }

        // Email provider is a HARD requirement in Production — a fail-fast, not
        // a warning. RequireConfirmedEmail=true means with no provider configured
        // the default ISender is NullSender: signup returns 200 ("check your
        // inbox"), but nothing is ever sent and the user can never log in. The
        // silent failure mode is worse than not starting.
        if (string.IsNullOrWhiteSpace(builder.Configuration["SENDGRID_API_KEY"])
            && string.IsNullOrWhiteSpace(builder.Configuration["FluentEmail:SendGridApiKey"])
            && string.IsNullOrWhiteSpace(builder.Configuration["EmailJs:ServiceId"])
            && string.IsNullOrWhiteSpace(builder.Configuration["EMAILJS_SERVICE_ID"]))
        {
            Console.WriteLine("[config] CRITICAL: Production requires an email provider because RequireConfirmedEmail=true. "
                + "Without it, users can sign up but cannot log in. Configure SENDGRID_API_KEY or EmailJS.");
            throw new InvalidOperationException(
                "Production requires an email provider because RequireConfirmedEmail=true. Without it, users can sign up but "
                + "cannot log in. Configure SENDGRID_API_KEY or EmailJS.");
        }

        if (string.IsNullOrWhiteSpace(builder.Configuration["Admin:Email"])
            || string.IsNullOrWhiteSpace(builder.Configuration["Admin:Password"]))
        {
            Console.WriteLine("[config] INFO: admin bootstrap credentials not set (Admin__Email / Admin__Password) — no admin account will be seeded.");
        }
    }
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddHttpClient(); // HttpClient factory for the EmailJS relay
builder.Services.AddHttpClient<MatchingClient>(c => c.Timeout = System.Threading.Timeout.InfiniteTimeSpan); // AI matching engine client
builder.Services.AddSingleton<EmailThrottleService>(); // per-email throttle for email-sending flows
builder.Services.AddRateLimiting(builder.Configuration); // API throttling (fixed-window per IP)
builder.Services.AddMemoryCache(); // short-TTL cache for expensive aggregate stats

builder.Services.AddInfrastructure(builder.Configuration); // PostgreSQL DbContext + AuthSeeder
builder.Services.SetupIdentity(builder.Configuration, builder.Environment); // Identity + JWT
builder.Services.AddControllers(); // Add API Controllers
builder.Services.AddNotificationInfrastructure(builder.Configuration); // Hangfire + Redis + SignalR + NotificationService

// All three pools draw on DefaultConnection but share nothing — count them
// together against Neon's ~20-connection free-tier ceiling at a glance.
// SignalR's 2 only apply when Redis is configured (one shared multiplexer:
// 1 pub/sub + 1 interactive connection).
{
    var hangfirePool = SeraGo.API.Extensions.ServicesExtensions.HangfireEnabled
        ? SeraGo.API.Extensions.ServicesExtensions.HangfireMaxPoolSize
        : 0;
    var signalRPool = string.IsNullOrWhiteSpace(builder.Configuration["REDIS_CONNECTION"])
        ? 0
        : SeraGo.API.Extensions.ServicesExtensions.SignalRBackplaneConnections;
    var total = SeraGo.Infrastructure.DependencyInjection.EfCoreMaxPoolSize + hangfirePool + signalRPool;
    Console.WriteLine($"[db] Pool budget vs Neon (~20 ceiling): " +
        $"EF={SeraGo.Infrastructure.DependencyInjection.EfCoreMaxPoolSize} " +
        $"+ Hangfire={hangfirePool} + SignalR={signalRPool} = {total}");
}

// Surface the main-DB connection source so connection-string issues are obvious at startup.
{
    var mainCs = builder.Configuration.GetConnectionString("DefaultConnection");
    var envUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(mainCs))
    {
        var host = new Npgsql.NpgsqlConnectionStringBuilder(mainCs).Host;
        Console.WriteLine($"[env] Main DB: using ConnectionStrings__DefaultConnection (host={host})");
    }
    else if (!string.IsNullOrWhiteSpace(envUrl))
    {
        Console.WriteLine($"[env] Main DB: using DATABASE_URL env var");
    }
    else
    {
        Console.WriteLine("[env] WARNING: no main DB connection string found in config or DATABASE_URL env var.");
    }
}

// Scraper database (Neon) connection for the admin job sync — read from the
// gitignored .env(SeraGo-Scraper) file, never from committed config.
builder.Services.AddSingleton(new ScraperDbOptions
{
    Host = Environment.GetEnvironmentVariable("DB_HOST") ?? string.Empty,
    Port = int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var port) ? port : 5432,
    Database = Environment.GetEnvironmentVariable("DB_NAME") ?? string.Empty,
    User = Environment.GetEnvironmentVariable("DB_USER") ?? string.Empty,
    Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty,
});

// Surface scraper-DB connectivity at startup so a missing .env is obvious
// (the sync endpoint 400s with the same message otherwise).
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_HOST")))
{
    Console.WriteLine($"[env] Scraper DB configured: {Environment.GetEnvironmentVariable("DB_HOST")}/{Environment.GetEnvironmentVariable("DB_NAME")}");
}
else
{
    Console.WriteLine("[env] WARNING: scraper DB credentials not found — admin job sync will be unavailable.");
}
// Auto-sync of scraped jobs. SYNC_ENABLED (default OFF — dev stays manual,
// the admin sync endpoint always works) turns on a scheduler that runs the
// sync at the UTC times in SYNC_SCHEDULE (default 09:15/20:45, right after
// the scraper's GitHub Actions runs at 09:00/20:30 UTC).
// SeraGo-AI service database (its own Neon DB) — read-only source for the
// admin AI-classification usage stats. Read from the gitignored
// .env(SeraGo-AI) file (AI_DB_* vars) or a full AI_DB_CONNECTION string.
builder.Services.AddSingleton(AiClassificationDbOptions.FromEnvironment());
{
    var aiDb = AiClassificationDbOptions.FromEnvironment();
    Console.WriteLine(aiDb.IsConfigured
        ? $"[env] SeraGo-AI DB configured: {aiDb.Host}/{aiDb.Database}"
        : "[env] SeraGo-AI DB not configured — /api/admin/ai/classification/stats will report no data.");
}

builder.Services.AddSingleton(new SyncOptions());
builder.Services.AddSingleton(new AdminApiOptions());
builder.Services.AddSingleton<ScrapedJobSyncService>();
builder.Services.AddSingleton<JobLifecycleCleanupService>(); // weekly deadline+7 cleanup
builder.Services.AddHostedService<SyncScheduler>();

builder.Services.AddScoped<ISectorNormalizer, SectorNormalizer>(); // sector standardization
builder.Services.AddSingleton<GroqClient>();                       // Groq AI client (Bearer auth from env)
builder.Services.AddScoped<JobClassificationService>();          // AI job classification (legacy — keeps the Groq call available for other uses)

// AI classification client — calls the decoupled SeraGo-AI classify API
// (POST /api/ai/classify) instead of calling the LLM directly.
builder.Services.Configure<AiClassificationOptions>(opt =>
{
    var cfg = AiClassificationOptionsSetup.FromEnvironment();
    opt.BaseUrl = cfg.BaseUrl;
    opt.ApiKey = cfg.ApiKey;
    if (opt.BaseUrl == null)
    {
        opt.BaseUrl = Environment.GetEnvironmentVariable("SERAGO_AI_URL") ?? "http://localhost:8001";
    }
});
builder.Services.AddHttpClient("SeraGoAiClassify"); // typed client base for the AI classify API
builder.Services.AddScoped<AiClassificationClient>();
builder.Services.AddScoped<AiClassificationSaveService>();
builder.Services.AddSingleton<R2StorageService>(); // Cloudflare R2 file storage
builder.Services.Configure<IdentityOptions>(options =>
{
    // Accounts are only usable after their email is confirmed — the signup and
    // forgot-password flows email a confirmation link (EmailJS), and the
    // frontend shows a "check your inbox" screen + dashboard guard. Google
    // accounts skip this: the OAuth handshake already confirms their email.
    options.SignIn.RequireConfirmedEmail = true;
});

var app = builder.Build();

// R2 readiness surfacing — a WARNING, never a throw. The service is lazy now
// (constructor can't throw), so a deploy without R2 env vars boots fine and
// upload requests get a clean 503 instead of crashing. In Production make the
// gap loud so it's fixed before users hit it.
if (app.Environment.IsProduction())
{
    var r2 = app.Services.GetRequiredService<R2StorageService>();
    if (!r2.IsConfigured)
    {
        app.Logger.LogWarning(
            "R2 storage not configured — avatar/resume/logo uploads will return 503. "
            + "Set R2_ACCESS_KEY_ID, R2_SECRET_ACCESS_KEY, CLOUDFLARE_ACCOUNT_ID, R2_BUCKET_NAME, R2_PUBLIC_URL.");
    }
}

// Apply pending migrations and seed roles + admin account on startup. The
// serverless database (Neon) can take a few seconds to wake from sleep, and
// connection resets are normal on flaky networks — retry transient failures
// instead of crashing the whole app on the first hiccup (EnableRetryOnFailure
// covers individual queries; this covers the whole migrate+seed phase).
const int maxStartupRetries = 5;
for (var attempt = 1; ; attempt++)
{
    try
    {
        // A fresh scope per attempt: after a connection-level reset the old
        // DbContext could hold a broken connection, which would make every
        // retry fail identically. A new scope gets a new pooled connection.
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AuthSeeder>().SeedAsync();
        break;
    }
    catch (Exception ex) when (attempt < maxStartupRetries && IsTransient(ex))
    {
        app.Logger.LogWarning(
            "Database not ready (attempt {Attempt}/{Max}): {Message}. Retrying in {Delay} seconds…",
            attempt, maxStartupRetries, ex.Message, attempt * 2);
        await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
    }
}

static bool IsTransient(Exception ex)
{
    for (var e = ex; e is not null; e = e.InnerException)
    {
        if (e is Npgsql.NpgsqlException or System.IO.IOException or System.Net.Sockets.SocketException)
        {
            return true;
        }
    }
    return false;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Standard response envelope — every endpoint (including Aufy's auth) returns
// { responseStatus, messageCode, message, data }. Registered after Swagger so
// the API docs stay unwrapped, before everything else so all responses (auth
// 401s, rate-limit 429s, unhandled 500s) come out in the one format.
app.UseMiddleware<ResponseEnvelopeMiddleware>();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies = { },
});

app.UseCors();
app.UseRateLimiter(); // throttling for endpoints that opt in via RequireRateLimiting
app.UseAuthentication();
app.UseAuthorization();

// Defense-in-depth: 404 any /api/admin/* request when the flag is off.
// Catches future endpoints registered outside the gate by convention failure.
var adminApiForMiddleware = app.Services.GetRequiredService<AdminApiOptions>();
app.UseMiddleware<AdminApiGateMiddleware>(adminApiForMiddleware.Enabled);

app.MapControllers();

app.MapProfileEndpoints();   // GET/PUT /api/account/profile — the user's own profile
app.MapAccountEndpoints();   // POST /api/account/deactivate, DELETE /api/account

app.MapJobEndpoints();   // /api/jobs — browse, search, post (draft flow), moderate
app.MapSavedJobEndpoints(); // /api/saved-jobs — save/unsave/list with lifecycle status
app.MapApplicationEndpoints(); // /api/applications — talent apply, recruiter manage
app.MapSectorEndpoints(); // /api/sectors (public list only — admin routes gated below)

// Admin endpoints — behind the kill switch. When ADMIN_API_ENABLED is off,
// none of these route groups are registered AND the gate middleware 404s
// any stray /api/admin/* path as defense in depth.
var adminApi = app.Services.GetRequiredService<AdminApiOptions>();
if (adminApi.Enabled)
{
    app.MapAdminEndpoints(); // sectors-admin, jobs-moderation, users, stats
}
app.MapFileUploadEndpoints(); // /api/upload — presigned URLs for file uploads
app.MapSettingsEndpoints();   // GET/PUT/PATCH /api/account/settings — user settings
app.MapNotificationEndpoints(); // /api/notifications — bell icon, unread count, mark read

// Telegram endpoints only when bot token is configured
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN"))
    || !string.IsNullOrWhiteSpace(builder.Configuration["TELEGRAM_BOT_TOKEN"]))
{
    app.MapTelegramEndpoints(); // /api/telegram — account linking, webhook
}

// SignalR hub for real-time notifications — keep-alive tuned for the Cloudflare
// proxy in front of this API. Cloudflare closes idle WebSocket connections after
// ~100 seconds, so the keep-alive ping must fire well inside that window (15s).
// ClientTimeoutInterval (30s) is the server-side idle cutoff before it considers
// a client gone; KeepAliveInterval (15s) is how often the server pings an idle
// connection to prove it's still there. Both are kept well under 60s so the
// connection survives the proxy timeout without relying on the client to send
// traffic first.
app.MapHub<NotificationHub>("/hubs/notifications");

// Hangfire dashboard (dev only — behind auth in production). Also skipped when
// HANGFIRE_ENABLED=false: no storage is registered, so the dashboard would fail.
if (app.Environment.IsDevelopment() && SeraGo.API.Extensions.ServicesExtensions.HangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire");
}

// Register recurring Hangfire jobs. Registration takes a distributed lock on
// the job storage; after a hard kill/restart the advisory lock can briefly
// outlive the previous instance (seen on the Neon pooler), making AddOrUpdate
// throw PostgreSqlDistributedLockException. Retry with backoff instead of
// crashing startup — AddOrUpdate is idempotent so re-running is safe.
// Skipped entirely when HANGFIRE_ENABLED=false (no storage registered).
if (SeraGo.API.Extensions.ServicesExtensions.HangfireEnabled)
{
    var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
    const int maxAttempts = 8;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            recurringJobs.AddOrUpdate<NotificationOrchestrator>(
                "notification-digest-emails",
                o => o.SendDigestEmails(),
                "0 8 * * *",  // daily at 8 AM UTC
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobs.AddOrUpdate<NotificationOrchestrator>(
                "notification-cleanup",
                o => o.CleanupOldNotifications(),
                "0 3 * * 0",  // weekly Sunday 3 AM UTC
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobs.AddOrUpdate<NotificationOrchestrator>(
                "notification-redis-sync",
                o => o.SyncRedisCounters(),
                "0 * * * *",  // hourly
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
            break;
        }
        catch (PostgreSqlDistributedLockException ex) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(5 * attempt);
            app.Logger.LogWarning(
                ex,
                "Hangfire recurring-job registration failed (attempt {Attempt}/{MaxAttempts}) — retrying in {DelaySeconds}s",
                attempt, maxAttempts, delay.TotalSeconds);
            Thread.Sleep(delay);
        }
    }
}

// Lightweight health check for the Render container health monitor.
// Deliberately stateless: no DB, Redis, R2, or external calls — safe to hit
// every 10 minutes without consuming meaningful resources. Excluded from auth,
// rate limiting, and the admin gate middleware, so it stays reachable even when
// ADMIN_API_ENABLED=false.
app.MapGet("/health", () => Results.Ok(new HealthResponse("ok")))
    .WithTags("Health")
    .Produces<HealthResponse>(StatusCodes.Status200OK);

app.MapGet("/", () => Results.Ok(new { service = "SeraGo API", docs = "/swagger" }));

app.Run();


public sealed record HealthResponse(string status);
