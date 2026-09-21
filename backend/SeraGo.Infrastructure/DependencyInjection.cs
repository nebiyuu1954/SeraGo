using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Effective EF Core max pool size after <see cref="AddInfrastructure"/> runs
    /// (0 until then). Consumed by the API's pool-total startup log so all three
    /// pools (EF Core, Hangfire, SignalR backplane) can be checked against
    /// Neon's free-tier connection ceiling at a glance.
    /// </summary>
    public static int EfCoreMaxPoolSize { get; private set; }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Try the standard config key (set via .env as
        //    ConnectionStrings__DefaultConnection=Host=…;Database=…;…).
        // 2. Fall back to the DATABASE_URL environment variable (the format
        //    Neon exposes in its dashboard: postgresql://user:pass@host/db).
        //    Npgsql can parse both key=value and URI formats natively.
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No database connection string found. " +
                "Set ConnectionStrings__DefaultConnection in your .env file (Npgsql key=value format) " +
                "or DATABASE_URL (postgresql:// URI). See .env.example for the expected format.");
        }

        // Validate that the connection string has a Host — Npgsql throws a
        // cryptic "Host can't be null" if it's missing. Catch it early with
        // an actionable message.
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            // Mask password in the logged value to avoid leaking credentials.
            var safe = connectionString.Length > 40 ? connectionString[..40] + "…" : connectionString;
            throw new InvalidOperationException(
                $"Connection string resolved but has no Host. " +
                $"Value (truncated): '{safe}'. " +
                $"Use key=value format: Host=…;Database=…;Username=…;Password=…;SslMode=Require");
        }

        // PostgreSQL (Neon). Connection string comes from configuration
        // (appsettings.json default, overridden by the DATABASE_URL / .env).
        //
        // Pool tuning: EF Core's default pool is 100, but Neon's free tier caps
        // at ~20 concurrent connections account-wide — beyond that, new
        // connections are rejected ("too many connections") and requests fail
        // randomly under moderate load. Three independent pools draw on this
        // connection string (EF Core, Hangfire, SignalR backplane) and share
        // nothing, so their caps must be budgeted to the ceiling together:
        // EF 12 + Hangfire 6 + SignalR backplane 2 = 20. Cap the pool to its
        // budget share, keep a couple of warm connections, and prune idle ones
        // fast. Values already present in the connection string are NOT
        // duplicated; DB_MAX_POOL_SIZE overrides the default (an explicit value
        // in the connection string wins over both). Rebuilding via the builder
        // keeps the output canonical (each key exactly once, Neon's sslmode
        // intact).
        var maxPoolSize = int.TryParse(Environment.GetEnvironmentVariable("DB_MAX_POOL_SIZE"), out var parsedMaxPoolSize) && parsedMaxPoolSize > 0
            ? parsedMaxPoolSize
            : 12;
        // Track effective values for the startup log — the builder's string
        // indexer THROWS "Keyword not supported" on missing keys, so it can't
        // be used to read back values that may be absent.
        var effMax = builder.ContainsKey("Max Pool Size") ? builder.MaxPoolSize : maxPoolSize;
        var effMin = builder.ContainsKey("Min Pool Size") ? builder.MinPoolSize : 2;
        var effIdle = builder.ContainsKey("Connection Idle Lifetime") ? builder.ConnectionIdleLifetime : 30;
        var effPrune = builder.ContainsKey("Connection Pruning Interval") ? builder.ConnectionPruningInterval : 10;
        if (!builder.ContainsKey("Max Pool Size"))
        {
            builder.MaxPoolSize = maxPoolSize;
        }
        if (!builder.ContainsKey("Min Pool Size"))
        {
            builder.MinPoolSize = 2;
        }
        if (!builder.ContainsKey("Connection Idle Lifetime"))
        {
            builder.ConnectionIdleLifetime = 30;
        }
        if (!builder.ContainsKey("Connection Pruning Interval"))
        {
            builder.ConnectionPruningInterval = 10;
        }
        var tunedConnectionString = builder.ConnectionString;

        // Exposed so the API's pool-total startup log can count all three pools
        // against the Neon ceiling at a glance. Set as a side effect of
        // registration — a config echo for logging, not runtime state.
        EfCoreMaxPoolSize = effMax;

        Console.WriteLine(
            $"[db] EF Core pool: max={effMax} min={effMin} idle={effIdle}s prune={effPrune}s "
            + (maxPoolSize != effMax ? $"(DB_MAX_POOL_SIZE={maxPoolSize} ignored — connection string wins)" : ""));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(tunedConnectionString, npgsql =>
            {
                // Serverless Postgres can reset connections while waking from
                // sleep or during network blips — retry transient failures
                // (connection resets, socket errors) instead of surfacing
                // random 500s. Recommended Npgsql/Neon setting.
                npgsql.EnableRetryOnFailure();
            }));

        services.AddScoped<AuthSeeder>();

        return services;
    }
}
