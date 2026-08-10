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

        // SQLite for now. Moving to PostgreSQL (Neon) later = change this one line
        // to UseNpgsql + add the Npgsql package, and update the connection string.
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<AuthSeeder>();

        return services;
    }
}
