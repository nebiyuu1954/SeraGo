namespace SeraGo.API.Middleware;

/// <summary>
/// Defense-in-depth gate: returns 404 for any request under /api/admin/*
/// when the admin API flag is OFF. This catches the failure mode where a
/// future developer registers a new admin endpoint outside the
/// <c>if (adminApi.Enabled)</c> block — the route might exist but this
/// middleware kills it at request time. When the flag is ON, this
/// middleware is a no-op passthrough.
/// </summary>
public sealed class AdminApiGateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _adminEnabled;

    public AdminApiGateMiddleware(RequestDelegate next, bool adminEnabled)
    {
        _next = next;
        _adminEnabled = adminEnabled;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_adminEnabled && context.Request.Path.StartsWithSegments("/api/admin"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new
            {
                responseStatus = "Failed",
                messageCode = "ADMIN_API_DISABLED",
                message = "Admin API is currently disabled.",
                data = (object?)null,
            });
            return;
        }

        await _next(context);
    }
}
