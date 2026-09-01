namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// In-app notification record. Source of truth for all notification channels.
/// Each row represents one notification event for one user.
/// Per-channel delivery (email, Telegram) is tracked via boolean flags.
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The user this notification belongs to.</summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    // ── What happened ──

    /// <summary>Event type — see <see cref="Core.Domain.Enums.NotificationType"/>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Short notification title (e.g. "New application received").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Human-readable body (e.g. "John Doe applied to Software Engineer").</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Structured JSON payload — job IDs, application IDs, actor info, etc.
    /// The frontend uses this to render type-specific UI and navigation.
    /// </summary>
    public string? Data { get; set; }

    // ── In-app read tracking ──

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    // ── Email delivery tracking ──

    public bool EmailSent { get; set; }
    public DateTimeOffset? EmailSentAt { get; set; }

    /// <summary>
    /// Groups notifications for digest emails. A batch of 5,000 job-match
    /// notifications created at 3 AM shares one batch_id; the 8 AM digest
    /// job queries by this to assemble per-user digest emails.
    /// </summary>
    public Guid? EmailBatchId { get; set; }

    // ── Telegram delivery tracking ──

    public bool TelegramSent { get; set; }
    public DateTimeOffset? TelegramSentAt { get; set; }

    // ── Metadata ──

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
