using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeraGo.API.Services;

/// <summary>
/// HTTP client for OpenRouter API (https://openrouter.ai/api/v1/).
/// Provides frontier reasoning models (e.g. NVIDIA Nemotron 3 Ultra 550B,
/// Google DeepMind Gemma 4 31B) with dual-account API key failover
/// to support 100+ requests per day on free tier accounts.
/// </summary>
public sealed class OpenRouterClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenRouterClient> _logger;
    private readonly List<string> _apiKeys = new();
    private readonly string _primaryModel;
    private readonly string _fallbackModel;
    private int _currentKeyIndex = 0;
    private readonly object _keyLock = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public OpenRouterClient(IHttpClientFactory factory, IConfiguration config, ILogger<OpenRouterClient> logger)
    {
        _logger = logger;

        // Collect all configured API keys (supports 4+ keys, comma-separated lists, and numbered keys)
        void AddKeys(string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return;
            var parts = candidate.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part) && !_apiKeys.Contains(part))
                {
                    _apiKeys.Add(part);
                }
            }
        }

        AddKeys(config["OPENROUTER_API_KEYS"] ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEYS"));
        AddKeys(config["OPENROUTER_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? config["OpenRouter:ApiKey"]);

        for (var i = 1; i <= 10; i++)
        {
            AddKeys(config[$"OPENROUTER_API_KEY_{i}"] ?? Environment.GetEnvironmentVariable($"OPENROUTER_API_KEY_{i}"));
            AddKeys(config[$"OpenRouter:ApiKey{i}"]);
        }

        AddKeys(config["OPENROUTER_API_KEY_SECONDARY"] ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY_SECONDARY") ?? config["OpenRouter:SecondaryApiKey"]);
        AddKeys(config["OPENROUTER_API_KEY_TERTIARY"] ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY_TERTIARY"));
        AddKeys(config["OPENROUTER_API_KEY_QUATERNARY"] ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY_QUATERNARY"));

        _primaryModel = config["OPENROUTER_MODEL"]
            ?? Environment.GetEnvironmentVariable("OPENROUTER_MODEL")
            ?? "nvidia/nemotron-3-ultra-550b-a55b:free";

        _fallbackModel = config["OPENROUTER_FALLBACK_MODEL"]
            ?? Environment.GetEnvironmentVariable("OPENROUTER_FALLBACK_MODEL")
            ?? "google/gemma-4-31b-it:free";

        var baseUrl = config["OPENROUTER_BASE_URL"]
            ?? Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL")
            ?? "https://openrouter.ai/api/v1/";

        var http = factory.CreateClient($"OpenRouter-{Guid.NewGuid():N}");
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Add("HTTP-Referer", "https://serago.pro.et");
        http.DefaultRequestHeaders.Add("X-Title", "SeraGo Job Engine");
        http.Timeout = TimeSpan.FromSeconds(60);
        _http = http;

        _logger.LogInformation(
            "OpenRouterClient initialized with {KeyCount} API key(s). Primary model: {PrimaryModel}, Fallback model: {FallbackModel}",
            _apiKeys.Count, _primaryModel, _fallbackModel);
    }

    public bool IsConfigured => _apiKeys.Count > 0;

    /// <summary>
    /// Executes a chat completion and deserializes the structured JSON response into <typeparamref name="TResponse"/>.
    /// Supports automatic failover between multiple OpenRouter accounts upon 429/quota limits,
    /// and fallback to secondary model upon upstream provider unavailability.
    /// </summary>
    public async Task<TResponse> ChatJsonAsync<TResponse>(
        string systemPrompt,
        string userPrompt,
        string? modelOverride = null,
        int timeoutSeconds = 35,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new AiServiceException("OpenRouter API key is not configured. Set OPENROUTER_API_KEY in environment variables.");
        }

        var targetModel = !string.IsNullOrWhiteSpace(modelOverride) ? modelOverride : _primaryModel;

        // Try primary model across available keys
        try
        {
            return await ExecuteWithKeyFailoverAsync<TResponse>(targetModel, systemPrompt, userPrompt, timeoutSeconds, ct);
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(_fallbackModel) && targetModel != _fallbackModel)
        {
            _logger.LogWarning(ex, "OpenRouter model {Model} failed. Attempting fallback model {FallbackModel}...", targetModel, _fallbackModel);
            return await ExecuteWithKeyFailoverAsync<TResponse>(_fallbackModel, systemPrompt, userPrompt, timeoutSeconds, ct);
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
            catch (OpenRouterRateLimitException ex)
            {
                lastException = ex;
                var delayMs = ex.RetryAfterSeconds.HasValue
                    ? ex.RetryAfterSeconds.Value * 1000
                    : Math.Min(2500 * (i + 1), 6000);

                _logger.LogWarning(
                    "OpenRouter API key #{KeyIndex} rate-limited or daily quota exceeded ({StatusCode}: {Message}). Waiting {DelayMs}ms before retry...",
                    keyIndex + 1, ex.StatusCode, ex.Message, delayMs);

                lock (_keyLock)
                {
                    // Advance to next key if available
                    _currentKeyIndex = (keyIndex + 1) % _apiKeys.Count;
                }

                if (i + 1 < attempts)
                {
                    _logger.LogInformation("Failing over to secondary OpenRouter key #{NextIndex} after {DelayMs}ms delay...", (_currentKeyIndex % _apiKeys.Count) + 1, delayMs);
                    await Task.Delay(delayMs, ct);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "OpenRouter request failed on key #{KeyIndex}.", keyIndex + 1);

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

        throw new AiServiceException($"All {_apiKeys.Count} OpenRouter key(s) failed after {attempts} attempts. Last error: {lastException?.Message}", lastException!);
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
            max_tokens = 4096,
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
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
            throw new AiServiceException($"OpenRouter timed out after {timeoutSeconds}s for model {model}");
        }

        var responseText = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            if (statusCode == 429 || statusCode == 402 || responseText.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || responseText.Contains("quota", StringComparison.OrdinalIgnoreCase))
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

                throw new OpenRouterRateLimitException(statusCode, $"OpenRouter rate limit or quota exceeded: {responseText}", retryAfter);
            }

            throw new AiServiceException($"OpenRouter returned {response.StatusCode}: {Truncate(responseText, 500)}");
        }

        var completion = JsonSerializer.Deserialize<OpenRouterCompletionResponse>(responseText, JsonOpts);
        if (completion?.Choices is null || completion.Choices.Count == 0)
        {
            throw new AiServiceException("OpenRouter returned a completion with no choices.");
        }

        var rawContent = completion.Choices[0].Message?.Content;
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            throw new AiServiceException("OpenRouter returned a completion with empty content.");
        }

        var cleanJson = CleanJsonContent(rawContent);

        try
        {
            return JsonSerializer.Deserialize<TResponse>(cleanJson, JsonOpts)
                ?? throw new AiServiceException("OpenRouter returned invalid JSON for the requested type.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("OpenRouter returned malformed JSON: {Content}", Truncate(cleanJson, 500));
            throw new AiServiceException("Failed to parse OpenRouter JSON: " + ex.Message, ex);
        }
    }

    private static string CleanJsonContent(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[7..];
        }
        else if (trimmed.StartsWith("```"))
        {
            trimmed = trimmed[3..];
        }

        if (trimmed.EndsWith("```"))
        {
            trimmed = trimmed[..^3];
        }

        return trimmed.Trim();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private sealed class OpenRouterCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenRouterChoice>? Choices { get; set; }
    }

    private sealed class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterMessage? Message { get; set; }
    }

    private sealed class OpenRouterMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}

public sealed class OpenRouterRateLimitException : Exception
{
    public int StatusCode { get; }
    public int? RetryAfterSeconds { get; }

    public OpenRouterRateLimitException(int statusCode, string message, int? retryAfterSeconds = null) : base(message)
    {
        StatusCode = statusCode;
        RetryAfterSeconds = retryAfterSeconds;
    }
}
