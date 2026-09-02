using Aufy.Core;
using Hangfire;
using Hangfire.Dashboard;
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

foreach (var envFile in new[] { ".env", ".env(SeraGo-Scraper)" })
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddHttpClient(); // HttpClient factory for the EmailJS relay
builder.Services.AddHttpClient<MatchingClient>(); // AI matching engine client
builder.Services.AddSingleton<EmailThrottleService>(); // per-email throttle for email-sending flows
builder.Services.AddRateLimiting(builder.Configuration); // API throttling (fixed-window per IP)

builder.Services.AddInfrastructure(builder.Configuration); // PostgreSQL DbContext + AuthSeeder
builder.Services.SetupAufy(builder.Configuration);         // Aufy: Identity + JWT + custom signup
builder.Services.AddNotificationInfrastructure(builder.Configuration); // Hangfire + Redis + SignalR + NotificationService

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
builder.Services.AddSingleton(new SyncOptions());
builder.Services.AddSingleton<ScrapedJobSyncService>();
builder.Services.AddSingleton<JobLifecycleCleanupService>(); // weekly deadline+7 cleanup
builder.Services.AddHostedService<SyncScheduler>();

builder.Services.AddScoped<ISectorNormalizer, SectorNormalizer>(); // sector standardization
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

app.UseCors();
app.UseRateLimiter(); // throttling for endpoints that opt in via RequireRateLimiting
app.UseAuthentication();
app.UseAuthorization();

app.MapAufyEndpoints();      // /api/auth/* and /api/account/* (login, signup, refresh, me, ...)
app.MapProfileEndpoints();   // GET/PUT /api/account/profile — the user's own profile
app.MapAccountEndpoints();   // POST /api/account/deactivate, DELETE /api/account
app.MapForgotPasswordEndpoint(); // POST /api/account/password/forgot — fixed replacement for Aufy 1.0.0's (500s on unknown emails)

// Replacements for Aufy endpoints removed from DI in ServicesExtensions.
app.MapSeraGoTokenEndpoint();          // POST /api/auth/token — lockout-aware sign-in errors
app.MapSeraGoExternalSignUpEndpoint(); // POST /api/auth/signup/external — links Google to existing email accounts
app.MapSeraGoWhoAmIEndpoint();         // GET /api/auth/whoami — adds emailConfirmed for the dashboard guard
app.MapSeraGoEmailConfirmationResendEndpoint(); // POST /api/account/email/confirm/resend — surfaces 404/409/200
app.MapSeraGoEmailConfirmEndpoint();             // GET /api/account/email/confirm — already-confirmed is a 200, not a 404

app.MapJobEndpoints();   // /api/jobs — browse, search, post (draft flow), moderate
app.MapSavedJobEndpoints(); // /api/saved-jobs — save/unsave/list with lifecycle status
app.MapApplicationEndpoints(); // /api/applications — talent apply, recruiter manage
app.MapSectorEndpoints(); // /api/sectors + admin sector management + scraped-job sync
app.MapStatsEndpoints();  // /api/admin/stats — top sectors + websites per period (scraper DB)
app.MapAdminUserEndpoints(); // /api/admin/users — admin user management
app.MapFileUploadEndpoints(); // /api/upload — presigned URLs for file uploads
app.MapSettingsEndpoints();   // GET/PUT/PATCH /api/account/settings — user settings
app.MapNotificationEndpoints(); // /api/notifications — bell icon, unread count, mark read

// Telegram endpoints only when bot token is configured
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN"))
    || !string.IsNullOrWhiteSpace(builder.Configuration["TELEGRAM_BOT_TOKEN"]))
{
    app.MapTelegramEndpoints(); // /api/telegram — account linking, webhook
}

// SignalR hub for real-time notifications
app.MapHub<NotificationHub>("/hubs/notifications");

// Hangfire dashboard (dev only — behind auth in production)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Register recurring Hangfire jobs
{
    var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
    var orchestrator = app.Services.GetRequiredService<IServiceScopeFactory>();

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
}

app.MapGet("/", () => Results.Ok(new { service = "SeraGo API", docs = "/swagger" }));

app.Run();
