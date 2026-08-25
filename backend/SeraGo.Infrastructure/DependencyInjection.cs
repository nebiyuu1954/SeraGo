using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

namespace SeraGo.Infrastructure;

public static class DependencyInjection
{
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
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
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
