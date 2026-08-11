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
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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
