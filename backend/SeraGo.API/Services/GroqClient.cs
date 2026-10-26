using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeraGo.API.Services;

/// <summary>
/// HTTP client for Groq's OpenAI-compat API (https://api.groq.com/openai/v1/).
///
/// Configure via env vars (never committed config):
///   GROQ_CONSOLE_API_KEY — Groq API key (console API key works for API calls).
///   GROQ_DEFAULT_MODEL   — fallback model when a service doesn't specify one
///                          (default: llama-3.1-8b-instant).
///
/// Mirrors the existing MatchingClient pattern: registered as a typed HttpClient
/// in Program.cs, best-effort by default — failures are logged and surfaced as
/// AiServiceException so callers can decide to skip/abort without breaking the
/// main request.
/// </summary>
public sealed class GroqClient
{
    private readonly HttpClient _http;
    private readonly ILogger<GroqClient> _logger;
    private readonly List<string> _apiKeys = new();
    private readonly string _defaultModel;
    private readonly string _fastFallbackModel = "llama-3.1-8b-instant";
    private int _currentKeyIndex = 0;
    private readonly object _keyLock = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public GroqClient(IHttpClientFactory factory, IConfiguration config, ILogger<GroqClient> logger)
    {
        _logger = logger;

        var primaryKey = config["GROQ_CONSOLE_API_KEY"]
            ?? Environment.GetEnvironmentVariable("GROQ_CONSOLE_API_KEY")
            ?? config["GROQ_API_KEY"]
            ?? Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? config["Groq:ApiKey"]
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(primaryKey))
        {
            var parts = primaryKey.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part) && !_apiKeys.Contains(part))
                {
                    _apiKeys.Add(part);
                }
            }
        }

        var secondaryKey = config["GROQ_API_KEY_SECONDARY"]
            ?? Environment.GetEnvironmentVariable("GROQ_API_KEY_SECONDARY")
            ?? config["GROQ_API_KEY_2"]
            ?? Environment.GetEnvironmentVariable("GROQ_API_KEY_2")
            ?? config["Groq:SecondaryApiKey"]
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(secondaryKey))
        {
            var parts = secondaryKey.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part) && !_apiKeys.Contains(part))
                {
                    _apiKeys.Add(part);
                }
            }
        }

        _defaultModel = config["GROQ_DEFAULT_MODEL"]
            ?? Environment.GetEnvironmentVariable("GROQ_DEFAULT_MODEL")
            ?? "llama-3.3-70b-versatile";

        var baseUrl = config["GROQ_BASE_URL"]
            ?? Environment.GetEnvironmentVariable("GROQ_BASE_URL")
            ?? "https://api.groq.com/openai/v1/";

        var http = factory.CreateClient($"Groq-{Guid.NewGuid():N}");
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.Timeout = TimeSpan.FromSeconds(60);
        _http = http;

        _logger.LogInformation(
            "GroqClient initialized with {KeyCount} API key(s). Default model: {DefaultModel}, Fast fallback: {FallbackModel}",
            _apiKeys.Count, _defaultModel, _fastFallbackModel);
    }

    public bool IsConfigured => _apiKeys.Count > 0;

    /// <summary>
    /// Chat-completion with structured JSON response. Supports multi-key rotation on 429
    /// and automatic fallback to llama-3.1-8b-instant (30k TPM) if 70B is rate-limited.
    /// </summary>
    public async Task<TResponse> ChatJsonAsync<TResponse>(
        string model,
        string systemPrompt,
        string userPrompt,
        int timeoutSeconds = 30,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new AiServiceException("Groq API key is not configured. Set GROQ_API_KEY in environment variables.");
        }

        var targetModel = !string.IsNullOrWhiteSpace(model) ? model : _defaultModel;

        try
        {
            return await ExecuteWithKeyFailoverAsync<TResponse>(targetModel, systemPrompt, userPrompt, timeoutSeconds, ct);
        }
        catch (Exception ex) when (targetModel != _fastFallbackModel)
        {
            _logger.LogWarning(ex, "Groq model {Model} failed or was rate-limited. Failing over to high-throughput {FallbackModel} (30k TPM)...", targetModel, _fastFallbackModel);
            return await ExecuteWithKeyFailoverAsync<TResponse>(_fastFallbackModel, systemPrompt, userPrompt, timeoutSeconds, ct);
        }
    }

    private async Task<TResponse> ExecuteWithKeyFailoverAsync<TResponse>(
        string model,
        string systemPrompt,
        string userPrompt,
        int timeoutSeconds,
        CancellationToken ct)
    {
        var attempts = Math.Max(_apiKeys.Count * 2, 2);
        Exception? lastException = null;

        for (var i = 0; i < attempts; i++)
        {
            string currentKey;
            int keyIndex;

            lock (_keyLock)
            {
                keyIndex = _currentKeyIndex % _apiKeys.Count;
                currentKey = _apiKeys[keyIndex];
            }

            try
            {
                return await SendChatRequestAsync<TResponse>(currentKey, model, systemPrompt, userPrompt, timeoutSeconds, ct);
            }
            catch (GroqRateLimitException ex)
            {
                lastException = ex;
                var delayMs = ex.RetryAfterSeconds.HasValue
                    ? ex.RetryAfterSeconds.Value * 1000
                    : Math.Min(2000 * (i + 1), 5000);

                _logger.LogWarning(
                    "Groq API key #{KeyIndex} rate-limited or TPM exceeded ({StatusCode}: {Message}). Waiting {DelayMs}ms...",
                    keyIndex + 1, ex.StatusCode, ex.Message, delayMs);

                lock (_keyLock)
                {
                    _currentKeyIndex = (keyIndex + 1) % _apiKeys.Count;
                }

                if (i + 1 < attempts)
                {
                    _logger.LogInformation("Failing over to Groq key #{NextIndex} after {DelayMs}ms delay...", (_currentKeyIndex % _apiKeys.Count) + 1, delayMs);
                    await Task.Delay(delayMs, ct);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Groq request failed on key #{KeyIndex}.", keyIndex + 1);

                lock (_keyLock)
                {
                    _currentKeyIndex = (keyIndex + 1) % _apiKeys.Count;
                }

                if (i + 1 < attempts)
                {
                    await Task.Delay(1000, ct);
                }
            }
        }

        throw new AiServiceException($"All {_apiKeys.Count} Groq key(s) failed for model {model}. Last error: {lastException?.Message}", lastException!);
    }

    private async Task<TResponse> SendChatRequestAsync<TResponse>(
        string apiKey,
        string model,
        string systemPrompt,
        string userPrompt,
        int timeoutSeconds,
        CancellationToken ct)
    {
        var payload = new
        {
            model = model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
            temperature = 0.0,
            response_format = new { type = "json_object" },
            max_tokens = 2048,
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = content;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new AiServiceException($"Groq timed out after {timeoutSeconds}s for model {model}");
        }

        var responseText = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            if (statusCode == 429 || responseText.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || responseText.Contains("tpm", StringComparison.OrdinalIgnoreCase))
            {
                int? retryAfter = null;
                if (response.Headers.RetryAfter?.Delta.HasValue == true)
                {
                    retryAfter = (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
                }
                else if (response.Headers.TryGetValues("Retry-After", out var vals) && int.TryParse(vals.FirstOrDefault(), out var parsedSec))
                {
                    retryAfter = parsedSec;
                }

                throw new GroqRateLimitException(statusCode, $"Groq rate limit or TPM exceeded: {responseText}", retryAfter);
            }

            throw new AiServiceException($"Groq returned {response.StatusCode}: {Truncate(responseText, 500)}");
        }

        var body = JsonSerializer.Deserialize<ChatCompletionResponse<TResponse>>(responseText, JsonOpts);
        if (body?.Choices is null || body.Choices.Count == 0)
        {
            throw new AiServiceException("Groq returned a completion with no choices.");
        }

        var raw = body.Choices[0].Message?.Content
            ?? throw new AiServiceException("Groq returned a completion with no content.");

        try
        {
            return JsonSerializer.Deserialize<TResponse>(raw, JsonOpts)
                ?? throw new AiServiceException("Groq returned invalid JSON for the chat completion.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Groq returned non-JSON content: {Content}", Truncate(raw, 500));
            throw new AiServiceException("Groq returned invalid JSON: " + ex.Message, ex);
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    // ── OpenAI-compat response shape ─────────────────────────────────

    private sealed class ChatCompletionResponse<T>
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}

/// <summary>
/// Wraps a failure from an AI provider (network, timeout, non-2xx, bad JSON).
/// Callers catch this to fall back to "no classification" rather than crashing.
/// </summary>
public sealed class AiServiceException : Exception
{
    public AiServiceException(string message) : base(message) { }
    public AiServiceException(string message, Exception inner) : base(message, inner) { }
}

public sealed class GroqRateLimitException : Exception
{
    public int StatusCode { get; }
    public int? RetryAfterSeconds { get; }

    public GroqRateLimitException(int statusCode, string message, int? retryAfterSeconds = null) : base(message)
    {
        StatusCode = statusCode;
        RetryAfterSeconds = retryAfterSeconds;
    }
}
