namespace SeraGo.API.Email;

/// <summary>
/// Per-email throttling for the flows that SEND email (forgot-password,
/// confirmation resend). Protects the email budget (EmailJS credits) from a
/// user hammering "reset password" / "resend confirmation" repeatedly.
///
/// In-memory sliding window: each address may receive at most
/// <c>MaxEmailsPerWindow</c> emails per <c>WindowMinutes</c>. Thread-safe,
/// registered as a singleton. Note: state is per-process and resets on
/// restart — fine for a single-instance app; swap for a distributed store
/// (Redis) if you scale to multiple instances.
///
/// Configuration (env var first, then appsettings fallback):
///   EMAIL_THROTTLE_ENABLED            (bool,  default true)
///   EMAIL_THROTTLE_MAX_PER_WINDOW     (int,   default 3)
///   EMAIL_THROTTLE_WINDOW_MINUTES     (int,   default 15)
/// </summary>
public sealed class EmailThrottleService
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Queue<DateTime>> _sends = new();
    private readonly int _maxEmailsPerWindow;
    private readonly TimeSpan _window;

    /// <summary>Throttle fully off — every send is allowed.</summary>
    public bool Enabled { get; }

    public EmailThrottleService(IConfiguration configuration)
    {
        Enabled = ParseBool(
            Read(configuration, "EMAIL_THROTTLE_ENABLED", "EmailThrottle:Enabled"), defaultValue: true);
        _maxEmailsPerWindow = Math.Max(1, ParseInt(
            Read(configuration, "EMAIL_THROTTLE_MAX_PER_WINDOW", "EmailThrottle:MaxEmailsPerWindow"),
            defaultValue: 3));
        _window = TimeSpan.FromMinutes(Math.Max(1, ParseInt(
            Read(configuration, "EMAIL_THROTTLE_WINDOW_MINUTES", "EmailThrottle:WindowMinutes"),
            defaultValue: 15)));
    }

    /// <summary>
    /// Returns true when an email to <paramref name="email"/> may be sent now,
    /// recording the send. Returns false (with a human-readable message) when
    /// the address is over its window budget.
    /// </summary>
    public bool TryAllow(string email, out string? retryAfterMessage)
    {
        retryAfterMessage = null;
        if (!Enabled)
        {
            return true;
        }

        var key = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            if (!_sends.TryGetValue(key, out var queue))
            {
                _sends[key] = queue = new Queue<DateTime>();
            }

            // Drop sends that have fallen out of the window.
            while (queue.Count > 0 && now - queue.Peek() >= _window)
            {
                queue.Dequeue();
            }

            if (queue.Count >= _maxEmailsPerWindow)
            {
                var retryAfter = _window - (now - queue.Peek());
                var minutes = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes));
                retryAfterMessage =
                    $"Too many emails sent to this address. Please try again in about {minutes} minute(s).";
                return false;
            }

            queue.Enqueue(now);
            return true;
        }
    }

    private static string? Read(IConfiguration configuration, string envKey, string configKey) =>
        configuration[envKey] is { Length: > 0 } envValue
            ? envValue
            : configuration[configKey];

    private static bool ParseBool(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) ? parsed : defaultValue;
}
