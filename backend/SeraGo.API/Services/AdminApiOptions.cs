namespace SeraGo.API.Services;

/// <summary>
/// Kill switch for the admin API surface. When OFF (env var
/// ADMIN_API_ENABLED not set or falsy), every admin endpoint is
/// unregistered at startup AND gated by middleware — the routes
/// don't exist, Swagger doesn't list them, and there's nothing
/// to enumerate or probe.
///
///   ADMIN_API_ENABLED  "true"/"1"/"yes"/"on" keeps admin APIs on.
///                      "false"/"0"/unset turns them off entirely (default).
///
/// Secure by default: an environment that forgets to set the flag gets
/// NO admin surface, not a public one. Enable it explicitly per environment.
/// </summary>
public sealed class AdminApiOptions
{
    public bool Enabled { get; }

    public AdminApiOptions()
    {
        var raw = Environment.GetEnvironmentVariable("ADMIN_API_ENABLED");
        // Default: OFF. Only an explicit truthy value exposes the admin APIs.
        Enabled = raw is not null
            && raw.Trim().ToLowerInvariant() is "1" or "true" or "yes" or "on";
    }
}
