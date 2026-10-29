using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeraGo.API.Services;

/// <summary>
/// HTTP client for DeepSeek API (https://api.deepseek.com/).
/// Serves as SeraGo's Tier 1 primary AI classifier using DeepSeek-V4 Flash (deepseek-flash).
/// Features:
///   - 2,500 concurrency limit
///   - Automatic context caching for system prompt
///   - Multi-key rotation support on 429
///   - OpenAI-compatible JSON mode chat completion
/// </summary>
public sealed class DeepSeekClient
{
    private readonly HttpClient _http;
    private readonly ILogger<DeepSeekClient> _logger;
    private readonly List<string> _apiKeys = new();
    private readonly string _model;
    private int _currentKeyIndex = 0;
    private readonly object _keyLock = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public DeepSeekClient(IHttpClientFactory factory, IConfiguration config, ILogger<DeepSeekClient> logger)
    {
        _logger = logger;

        // Collect all configured DeepSeek API keys
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

        AddKeys(config["DEEPSEEK_API_KEYS"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEYS"));
        AddKeys(config["DEEPSEEK_API_KEY"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? config["DeepSeek:ApiKey"]);

        for (var i = 1; i <= 5; i++)
        {
            AddKeys(config[$"DEEPSEEK_API_KEY_{i}"] ?? Environment.GetEnvironmentVariable($"DEEPSEEK_API_KEY_{i}"));
            AddKeys(config[$"DeepSeek:ApiKey{i}"]);
        }

        AddKeys(config["DEEPSEEK_API_KEY_SECONDARY"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY_SECONDARY") ?? config["DeepSeek:SecondaryApiKey"]);

        _model = config["DEEPSEEK_MODEL"]
            ?? Environment.GetEnvironmentVariable("DEEPSEEK_MODEL")
            ?? "deepseek-flash";

        var baseUrl = config["DEEPSEEK_BASE_URL"]
            ?? Environment.GetEnvironmentVariable("DEEPSEEK_BASE_URL")
            ?? "https://api.deepseek.com/";

        var http = factory.CreateClient($"DeepSeek-{Guid.NewGuid():N}");
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.Timeout = TimeSpan.FromSeconds(60);
        _http = http;

        _logger.LogInformation(
            "DeepSeekClient initialized with {KeyCount} API key(s). Model: {Model}, BaseUrl: {BaseUrl}",
            _apiKeys.Count, _model, baseUrl);
    }

    public bool IsConfigured => _apiKeys.Count > 0;

    /// <summary>
    /// Executes a chat completion against DeepSeek and deserializes the structured JSON response into <typeparamref name="TResponse"/>.
    /// Supports automatic key failover if multiple keys are provided.
    /// </summary>
    public async Task<TResponse> ChatJsonAsync<TResponse>(
        string systemPrompt,
        string userPrompt,
        string? modelOverride = null,
        int timeoutSeconds = 40,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new AiServiceException("DeepSeek API key is not configured. Set DEEPSEEK_API_KEY in environment variables.");
        }

        var targetModel = !string.IsNullOrWhiteSpace(modelOverride) ? modelOverride : _model;
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
                return await SendChatRequestAsync<TResponse>(currentKey, targetModel, systemPrompt, userPrompt, timeoutSeconds, ct);
            }
            catch (DeepSeekRateLimitException ex)
            {
                lastException = ex;
                var delayMs = ex.RetryAfterSeconds.HasValue
                    ? ex.RetryAfterSeconds.Value * 1000
                    : Math.Min(2000 * (i + 1), 5000);

                _logger.LogWarning(
                    "DeepSeek API key #{KeyIndex} rate-limited or quota exceeded ({StatusCode}: {Message}). Waiting {DelayMs}ms before retry...",
                    keyIndex + 1, ex.StatusCode, ex.Message, delayMs);

                lock (_keyLock)
                {
                    _currentKeyIndex = (keyIndex + 1) % _apiKeys.Count;
                }

                if (i + 1 < attempts)
                {
                    await Task.Delay(delayMs, ct);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "DeepSeek request failed on key #{KeyIndex}.", keyIndex + 1);

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

        throw new AiServiceException($"All {_apiKeys.Count} DeepSeek key(s) failed after {attempts} attempts. Last error: {lastException?.Message}", lastException!);
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
            stream = false,
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
            throw new AiServiceException($"DeepSeek timed out after {timeoutSeconds}s for model {model}");
        }

        var responseText = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            if (statusCode == 429 || statusCode == 402 || statusCode == 503 || responseText.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || responseText.Contains("balance", StringComparison.OrdinalIgnoreCase))
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

                throw new DeepSeekRateLimitException(statusCode, $"DeepSeek rate limit, balance, or overload ({statusCode}): {responseText}", retryAfter);
            }

            throw new AiServiceException($"DeepSeek returned {response.StatusCode}: {Truncate(responseText, 500)}");
        }

        var completion = JsonSerializer.Deserialize<DeepSeekCompletionResponse>(responseText, JsonOpts);
        if (completion?.Choices is null || completion.Choices.Count == 0)
        {
            throw new AiServiceException("DeepSeek returned a completion with no choices.");
        }

        var rawContent = completion.Choices[0].Message?.Content;
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            throw new AiServiceException("DeepSeek returned a completion with empty content.");
        }

        var cleanJson = ExtractJsonContent(rawContent);

        try
        {
            var deserialized = JsonSerializer.Deserialize<TResponse>(cleanJson, JsonOpts);
            if (deserialized is null)
            {
                throw new AiServiceException("Deserialized DeepSeek response was null.");
            }
            return deserialized;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize DeepSeek JSON output: {RawContent}", Truncate(rawContent, 1000));
            throw new AiServiceException($"DeepSeek returned invalid JSON matching the schema: {ex.Message}", ex);
        }
    }

    private static string ExtractJsonContent(string text)
    {
        var trimmed = text.Trim();

        // Strip ```json ... ``` code fence
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            var endIdx = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (endIdx > 7)
            {
                return trimmed[7..endIdx].Trim();
            }
        }
        else if (trimmed.StartsWith("```"))
        {
            var endIdx = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (endIdx > 3)
            {
                return trimmed[3..endIdx].Trim();
            }
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return trimmed;
    }

    private static string Truncate(string? val, int maxLen)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Length <= maxLen ? val : val[..maxLen] + "...";
    }
}

public class DeepSeekRateLimitException : Exception
{
    public int StatusCode { get; }
    public int? RetryAfterSeconds { get; }

    public DeepSeekRateLimitException(int statusCode, string message, int? retryAfterSeconds = null)
        : base(message)
    {
        StatusCode = statusCode;
        RetryAfterSeconds = retryAfterSeconds;
    }
}

internal sealed class DeepSeekCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<DeepSeekChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public DeepSeekUsage? Usage { get; set; }
}

internal sealed class DeepSeekChoice
{
    [JsonPropertyName("message")]
    public DeepSeekMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

internal sealed class DeepSeekMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("reasoning_content")]
    public string? ReasoningContent { get; set; }
}

internal sealed class DeepSeekUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
