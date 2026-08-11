using Aufy.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Email;
using SeraGo.API.Endpoints;
using SeraGo.API.Extensions;
using SeraGo.Infrastructure;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

// Load .env before configuration is built — .NET has no native .env support.
// Searches the working directory and its parents, so this works whether the
// API is started from backend/SeraGo.API or the repo root. Existing process
// env vars win over .env (NoClobber), so real secrets always take precedence.
var envDir = new DirectoryInfo(Directory.GetCurrentDirectory());
while (envDir is not null)
{
    var envPath = Path.Combine(envDir.FullName, ".env");
    if (File.Exists(envPath))
    {
        DotNetEnv.Env.Load(envPath, DotNetEnv.Env.NoClobber());
        break;
    }
    envDir = envDir.Parent;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddHttpClient(); // HttpClient factory for the EmailJS relay
builder.Services.AddSingleton<EmailThrottleService>(); // per-email throttle for email-sending flows

builder.Services.AddInfrastructure(builder.Configuration); // SQLite DbContext + AuthSeeder
builder.Services.SetupAufy(builder.Configuration);         // Aufy: Identity + JWT + custom signup
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

app.UseCors();
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

app.MapGet("/", () => Results.Ok(new { service = "SeraGo API", docs = "/swagger" }));

app.Run();
