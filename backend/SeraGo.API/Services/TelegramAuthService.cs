using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SeraGo.API.Services;

public sealed record TelegramAuthPayload(
    long Id,
    string? FirstName,
    string? LastName,
    string? Username,
    string? PhotoUrl,
    long AuthDate,
    string Hash);

public interface ITelegramAuthService
{
    bool Verify(TelegramAuthPayload payload);
    string GenerateStateToken(TelegramAuthPayload payload);
    TelegramAuthPayload? ValidateStateToken(string token);
}

public sealed class TelegramAuthService : ITelegramAuthService
{
    private readonly string? _botToken;
    private readonly string _stateSecret;
    private readonly ILogger<TelegramAuthService> _logger;

    public TelegramAuthService(IConfiguration configuration, ILogger<TelegramAuthService> logger)
    {
        _logger = logger;
        _botToken = configuration["TELEGRAM_BOT_TOKEN"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

        _stateSecret = configuration["TELEGRAM_LINK_SECRET"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_LINK_SECRET")
            ?? configuration["INTERNAL_API_KEY"]
            ?? Environment.GetEnvironmentVariable("INTERNAL_API_KEY")
            ?? _botToken
            ?? "serago-telegram-state-secret";
    }

    /// <summary>
    /// Verifies Telegram Login Widget / Auth data according to the official specification:
    /// https://core.telegram.org/widgets/login#checking-authorization
    /// </summary>
    public bool Verify(TelegramAuthPayload payload)
    {
        if (string.IsNullOrWhiteSpace(_botToken))
        {
            _logger.LogWarning("Telegram bot token is not configured; cannot verify Telegram auth.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload.Hash))
        {
            return false;
        }

        // Freshness guard: check that auth_date is within the last 24 hours
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (payload.AuthDate > now + 300 || now - payload.AuthDate > 86400)
        {
            _logger.LogWarning("Telegram auth payload timestamp expired or in the future: auth_date={AuthDate}, now={Now}", payload.AuthDate, now);
            return false;
        }

        // Build data_check_string from alphabetical key-value pairs
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
        fields["auth_date"] = payload.AuthDate.ToString();
        fields["id"] = payload.Id.ToString();

        if (!string.IsNullOrWhiteSpace(payload.FirstName))
            fields["first_name"] = payload.FirstName;
        if (!string.IsNullOrWhiteSpace(payload.LastName))
            fields["last_name"] = payload.LastName;
        if (!string.IsNullOrWhiteSpace(payload.Username))
            fields["username"] = payload.Username;
        if (!string.IsNullOrWhiteSpace(payload.PhotoUrl))
            fields["photo_url"] = payload.PhotoUrl;

        var sb = new StringBuilder();
        foreach (var (k, v) in fields)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(k).Append('=').Append(v);
        }
        var dataCheckString = sb.ToString();

        // Secret key is SHA256 of bot token (raw 32 bytes)
        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(_botToken));

        // HMAC-SHA256 of data_check_string with secret key
        var hmac = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
        var computedHex = Convert.ToHexString(hmac).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(payload.Hash.ToLowerInvariant()));
    }

    /// <summary>
    /// Generates a tamper-proof state token for Telegram signups so the profile completion
    /// step doesn't need to re-verify or trust unverified client fields.
    /// Format: base64url(json) + "." + base64url(hmac)
    /// </summary>
    public string GenerateStateToken(TelegramAuthPayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var jsonB64 = Base64UrlEncode(jsonBytes);

        var hmac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(_stateSecret), jsonBytes);
        var hmacB64 = Base64UrlEncode(hmac);

        return $"{jsonB64}.{hmacB64}";
    }

    public TelegramAuthPayload? ValidateStateToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2) return null;

            var jsonBytes = Base64UrlDecode(parts[0]);
            var expectedHmac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(_stateSecret), jsonBytes);
            var actualHmac = Base64UrlDecode(parts[1]);

            if (!CryptographicOperations.FixedTimeEquals(expectedHmac, actualHmac))
            {
                return null;
            }

            var payload = JsonSerializer.Deserialize<TelegramAuthPayload>(jsonBytes);
            if (payload == null) return null;

            // Enforce max 30 minutes lifetime on signup state token
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - payload.AuthDate > 1800)
            {
                return null;
            }

            return payload;
        }
        catch
        {
            return null;
        }
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

    private static byte[] Base64UrlDecode(string str)
    {
        var padded = str.Replace("-", "+").Replace("_", "/");
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded,
        };
        return Convert.FromBase64String(padded);
    }
}
