namespace SeraGo.API.Services;

/// <summary>
/// Kill switch for the admin API surface. When OFF (env var
/// ADMIN_API_ENABLED not set or falsy), every admin endpoint is
/// unregistered at startup AND gated by middleware — the routes
/// don't exist, Swagger doesn't list them, and there's nothing
/// to enumerate or probe.
///
///   ADMIN_API_ENABLED  "true"/"1" keeps admin APIs on (default).
///                      "false"/"0" /unset turns them off entirely.
/// </summary>
public sealed class AdminApiOptions
{
    public bool Enabled { get; }

    public AdminApiOptions()
    {
        var raw = Environment.GetEnvironmentVariable("ADMIN_API_ENABLED");
        // Default: ON (admin APIs visible). Only explicitly disabling them hides everything.
        Enabled = string.IsNullOrWhiteSpace(raw)
            || raw.Trim().ToLowerInvariant() is "1" or "true" or "yes" or "on";
    }
}
