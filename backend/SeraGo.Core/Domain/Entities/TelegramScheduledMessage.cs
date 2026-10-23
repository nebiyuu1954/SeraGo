namespace SeraGo.Core.Domain.Entities;

public enum TelegramMessageTarget
{
    Channel = 1,
    BotDm = 2,
    Both = 3
}

public enum TelegramMessageType
{
    GoodMorning = 1,
    GoodAfternoon = 2,
    GoodNight = 3,
    CustomAd = 4,
    CustomMessage = 5
}

/// <summary>
/// A scheduled or automated recurring Telegram message broadcast to the public channel
/// and/or personalized direct messages to linked bot users.
/// </summary>
public class TelegramScheduledMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Friendly admin title / label (e.g. "Weekday Morning Job Pulse")</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Broadcast target: Channel, BotDm, or Both</summary>
    public TelegramMessageTarget Target { get; set; } = TelegramMessageTarget.Both;

    /// <summary>Message purpose and template generator style</summary>
    public TelegramMessageType MessageType { get; set; } = TelegramMessageType.GoodMorning;

    /// <summary>
    /// JSON array of active days, e.g. ["Monday","Tuesday","Wednesday","Thursday","Friday"] or ["*"] for everyday.
    /// </summary>
    public string DaysOfWeek { get; set; } = "[\"*\"]";

    /// <summary>
    /// Optional specific date in "YYYY-MM-DD" format for one-off holiday or announcement broadcasts.
    /// When specified, only fires on this exact calendar date.
    /// </summary>
    public string? SpecificDate { get; set; }

    /// <summary>
    /// Scheduled execution time in East Africa Time (EAT / UTC+3) in "HH:mm" 24h format (e.g. "08:00", "13:30", "20:30").
    /// </summary>
    public string ScheduledTime { get; set; } = "08:00";

    /// <summary>
    /// Custom template with placeholder support (e.g. {user_name}, {today_count}, {top_sectors}).
    /// If null or empty, the built-in rich dynamic generator for the MessageType is used.
    /// </summary>
    public string? CustomTemplate { get; set; }

    /// <summary>Optional custom banner image URL. Defaults to source/SeraGo branding when null.</summary>
    public string? BannerUrl { get; set; }

    /// <summary>Optional label for the inline CTA button (e.g. "Explore Today's Jobs")</summary>
    public string? ButtonText { get; set; }

    /// <summary>Optional URL destination for the CTA button</summary>
    public string? ButtonUrl { get; set; }

    /// <summary>Whether this schedule is currently enabled</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When this message was last dispatched</summary>
    public DateTimeOffset? LastRunAt { get; set; }

    /// <summary>Result status of the last run: Success, Partial, Failed</summary>
    public string? LastRunStatus { get; set; }

    /// <summary>How many recipients (channels + users) were reached in the last dispatch</summary>
    public int? LastRunRecipientCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
