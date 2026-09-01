namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// 1:1 per-user settings — stored as a flexible JSON blob so new settings
/// never require a migration. The frontend schema controls the shape; the
/// backend treats it as opaque JSON with version-based optimistic concurrency.
///
/// External services (AI, notifications) read settings via internal API
/// endpoints using the same JSON structure.
/// </summary>
public class UserSettings
{
    /// <summary>Same PK as the owning user — shared-primary-key 1:1.</summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// The settings payload. Structure is versioned — see SettingsVersion.
    /// Example shape:
    /// {
    ///   "version": 1,
    ///   "account": { "language": "en" },
    ///   "notifications": {
    ///     "email": { "jobAlerts": true, ... },
    ///     "inApp": { "jobAlerts": true, ... }
    ///   },
    ///   "security": { ... },
    ///   "ai": { "enableAiMatching": true, ... }
    /// }
    /// </summary>
    public string Settings { get; set; } = "{}";

    /// <summary>Optimistic concurrency version — bumped on every save.</summary>
    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
