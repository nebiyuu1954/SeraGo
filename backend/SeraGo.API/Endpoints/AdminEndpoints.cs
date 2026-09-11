namespace SeraGo.API.Endpoints;

/// <summary>
/// Central registration point for all admin-only endpoint groups.
/// Called once from Program.cs inside the <c>if (adminApi.Enabled)</c> gate.
/// Splitting admin routes here (instead of scattering them across every
/// feature file) means turning off <c>ADMIN_API_ENABLED</c> unregisters
/// them all in one shot — zero attack surface, nothing enumerable.
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAdminSectorEndpoints();   // /api/admin/sectors + sync
        app.MapAdminJobEndpoints();      // /api/jobs/{id}/approve|reject|restore|sector
        app.MapAdminUserEndpoints();     // /api/admin/users (already separate file)
        app.MapStatsEndpoints();                  // /api/admin/stats/top
        app.MapAdminStatsOverviewEndpoint();      // /api/admin/stats/overview
        app.MapAdminJobStatsEndpoints();          // /api/admin/jobs/views
        app.MapAdminJobViewsStatsEndpoint();      // /api/admin/jobs/views/stats
        app.MapAdminScraperEndpoints();          // /api/admin/scraper/week-stats + /week/{date}
        app.MapAdminJobClassificationEndpoints(); // /api/admin/jobs/classify (AI sector classification)
        app.MapAdminAiClassificationStatsEndpoint(); // /api/admin/ai/classification/stats (AI usage + token totals)
        // Future admin groups go here:
        // app.MapAdminApplicationEndpoints();
        // app.MapAdminScraperEndpoints();
        // app.MapAdminAnnouncementEndpoints();
        return app;
    }
}
