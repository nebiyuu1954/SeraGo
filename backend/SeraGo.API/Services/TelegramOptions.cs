namespace SeraGo.API.Services;

/// <summary>
/// Configuration for the Telegram job surfaces — the public channel broadcast
/// and the personalized job DMs. Read from environment variables (loaded from
/// .env at startup, same as every other secret in this app).
///
///   TELEGRAM_CHANNEL_ID              channel to broadcast published jobs to
///                                    ("@name" or "-100…"). Unset = no channel.
///   TELEGRAM_CHANNEL_ENABLED         "true"/"1"/"yes"/"on" force-enables.
///                                    Default: enabled when a channel id is set.
///   TELEGRAM_JOB_DM_MODE             ai | sector | both | off   (default both)
///   TELEGRAM_AI_SCORE_THRESHOLD      0-100 min score to DM in ai mode (default 60)
///   TELEGRAM_JOB_DM_MAX_RECIPIENTS   per-job recipient cap (default 200)
///   TELEGRAM_CHANNEL_MAX_PER_RUN     channel posts per sync run (default 40)
///   TELEGRAM_JOB_LINK_BASE           click-through base URL
/// </summary>
public sealed class TelegramOptions
{
    public string? ChannelId { get; }

    /// <summary>True when channel posting is on (explicit flag, or a channel id is present).</summary>
    public bool ChannelEnabled { get; }

    /// <summary>ai | sector | both | off</summary>
    public string JobDmMode { get; }

    public double AiScoreThreshold { get; }
    public int JobDmMaxRecipients { get; }
    public int ChannelMaxPerRun { get; }
    public string JobLinkBase { get; }

    /// <summary>Window in seconds across which synced jobs are paced out to the channel (default: 900s / 15min).</summary>
    public int ChannelPacingWindowSeconds { get; }

    /// <summary>Minimum delay between channel posts in seconds (default: 4s to respect Telegram limits).</summary>
    public double ChannelMinDelaySeconds { get; }

    /// <summary>Maximum delay between channel posts in seconds (default: 180s / 3min).</summary>
    public double ChannelMaxDelaySeconds { get; }

    /// <summary>When true, the scraper sync also fans out personalized DMs to subscribed talents.</summary>
    public bool SyncDmEnabled { get; }

    /// <summary>Max newly-synced jobs to evaluate for DMs per sync run (default: 30).</summary>
    public int SyncDmMaxJobs { get; }

    /// <summary>Max DMs a single talent can receive per sync run (default: 3) to prevent notification bursts.</summary>
    public int TalentDmMaxPerUserPerRun { get; }

    /// <summary>Channel posting is active when enabled and an id exists.</summary>
    public bool WantsChannel => ChannelEnabled && !string.IsNullOrWhiteSpace(ChannelId);

    /// <summary>Sector-based DMs are part of the current mode.</summary>
    public bool AllowsSectorDm => JobDmMode is "sector" or "both";

    /// <summary>AI score-based DMs are part of the current mode.</summary>
    public bool AllowsAiDm => JobDmMode is "ai" or "both";

    // ── Dedicated Testing Configuration ──
    /// <summary>Testing channel (e.g. "@SeraGo_Test_Channel" or "-100..."). Used for test broadcasts.</summary>
    public string? TestChannelId { get; }

    /// <summary>Specific Telegram user chatId for DM tests.</summary>
    public long? TestChatId { get; }

    /// <summary>Optional separate test bot token; if omitted, uses standard TELEGRAM_BOT_TOKEN.</summary>
    public string? TestBotToken { get; }

    /// <summary>When true, all scheduled messaging routes strictly to test channel / test chat.</summary>
    public bool MessagingTestMode { get; }

    public bool HasTestChannel => !string.IsNullOrWhiteSpace(TestChannelId);
    public bool HasTestChat => TestChatId.HasValue && TestChatId.Value != 0;

    public TelegramOptions()
    {
        ChannelId = Env("TELEGRAM_CHANNEL_ID");

        // Default: enabled when a channel id is configured. An explicit
        // TELEGRAM_CHANNEL_ENABLED=false forces it off even with an id set.
        var enabledRaw = Env("TELEGRAM_CHANNEL_ENABLED");
        ChannelEnabled = enabledRaw is not null
            ? IsTruthy(enabledRaw)
            : !string.IsNullOrWhiteSpace(ChannelId);

        var mode = (Env("TELEGRAM_JOB_DM_MODE") ?? "both").Trim().ToLowerInvariant();
        JobDmMode = mode is "ai" or "sector" or "both" or "off" ? mode : "both";

        AiScoreThreshold = ParseDouble(Env("TELEGRAM_AI_SCORE_THRESHOLD"), 60);
        JobDmMaxRecipients = ParseInt(Env("TELEGRAM_JOB_DM_MAX_RECIPIENTS"), 200);
        ChannelMaxPerRun = ParseInt(Env("TELEGRAM_CHANNEL_MAX_PER_RUN"), 40);
        JobLinkBase = (Env("TELEGRAM_JOB_LINK_BASE") ?? "https://serago.pro.et/dashboard/talent/jobs").TrimEnd('/');

        ChannelPacingWindowSeconds = ParseInt(Env("TELEGRAM_CHANNEL_PACING_WINDOW_SECONDS"), 900);
        ChannelMinDelaySeconds = ParseDouble(Env("TELEGRAM_CHANNEL_MIN_DELAY_SECONDS"), 4.0);
        ChannelMaxDelaySeconds = ParseDouble(Env("TELEGRAM_CHANNEL_MAX_DELAY_SECONDS"), 180.0);

        var syncDmRaw = Env("TELEGRAM_SYNC_DM_ENABLED");
        SyncDmEnabled = syncDmRaw is not null ? IsTruthy(syncDmRaw) : true;
        SyncDmMaxJobs = ParseInt(Env("TELEGRAM_SYNC_DM_MAX_JOBS"), 30);
        TalentDmMaxPerUserPerRun = ParseInt(Env("TELEGRAM_TALENT_DM_MAX_PER_RUN"), 3);

        TestChannelId = Env("TELEGRAM_TEST_CHANNEL_ID");
        if (long.TryParse(Env("TELEGRAM_TEST_CHAT_ID"), out var parsedChatId))
        {
            TestChatId = parsedChatId;
        }
        TestBotToken = Env("TELEGRAM_TEST_BOT_TOKEN");
        MessagingTestMode = IsTruthy(Env("TELEGRAM_MESSAGING_TEST_MODE") ?? "false");
    }

    private static string? Env(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool IsTruthy(string raw) =>
        raw.Trim().ToLowerInvariant() is "1" or "true" or "yes" or "on";

    private static int ParseInt(string? raw, int fallback) =>
        int.TryParse(raw, out var value) && value > 0 ? value : fallback;

    private static double ParseDouble(string? raw, double fallback) =>
        double.TryParse(raw, out var value) && value is >= 0 and <= 100 ? value : fallback;
}
