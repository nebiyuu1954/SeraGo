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
    private readonly string _apiKey;
    private readonly string _defaultModel;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Keep the JSON compact — prompts carry a lot of text.
        WriteIndented = false,
    };

    public GroqClient(IHttpClientFactory factory, IConfiguration config, ILogger<GroqClient> logger)
    {
        _logger = logger;
        _apiKey = config["GROQ_CONSOLE_API_KEY"]
            ?? Environment.GetEnvironmentVariable("GROQ_CONSOLE_API_KEY")
            ?? throw new InvalidOperationException(
                "GROQ_CONSOLE_API_KEY is not configured. Set it in .env to enable AI services.");

        _defaultModel = config["GROQ_DEFAULT_MODEL"]
            ?? Environment.GetEnvironmentVariable("GROQ_DEFAULT_MODEL")
            ?? "openai/gpt-oss-120b";

        var baseUrl = config["GROQ_BASE_URL"]
            ?? Environment.GetEnvironmentVariable("GROQ_BASE_URL")
            ?? "https://api.groq.com/openai/v1/";

        var http = factory.CreateClient($"Groq-{Guid.NewGuid():N}");
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        http.Timeout = TimeSpan.FromSeconds(60);
        _http = http;
    }

    /// <summary>
    /// Posts a JSON body with an explicit Content-Length (via StringContent).
    /// PostAsJsonAsync/JsonContent sends `Transfer-Encoding: chunked` instead,
    /// which some servers don't decode cleanly — this keeps the body intact.
    /// </summary>
    private async Task<TResponse> PostAsync<TResponse>(
        string path, object body, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));

        try
        {
            var response = await _http.PostAsync(path, content, timeoutCts.Token);
            var text = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Groq call failed: {Path} → {StatusCode} body={Body}",
                    path, response.StatusCode, Truncate(text, 500));
                throw new AiServiceException(
                    $"Groq returned {response.StatusCode} for {path}. Body: {text}");
            }

            return JsonSerializer.Deserialize<TResponse>(text, JsonOpts)
                ?? throw new AiServiceException($"Groq returned empty response for {path}");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Groq call timed out: {Path}", path);
            throw new AiServiceException($"Groq timed out for {path}");
        }
    }

    /// <summary>
    /// Chat-completion with a structured JSON response. The model is told to
    /// return ONLY a JSON object matching <typeparamref name="TResponse"/>;
    /// the response is deserialized and returned. On parse failure an
    /// AiServiceException is thrown so the caller can treat it as "unclassified"
    /// rather than silently using garbage.
    /// </summary>
    public async Task<TResponse> ChatJsonAsync<TResponse>(
        string model,
        string systemPrompt,
        string userPrompt,
        int timeoutSeconds = 30,
        CancellationToken ct = default)
    {
        var payload = new
        {
            model = model ?? _defaultModel,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
            temperature = 0.0,
            // Groq supports response_format json_object for structured output.
            response_format = new { type = "json_object" },
            max_tokens = 2048,
        };

        var body = await PostAsync<ChatCompletionResponse<TResponse>>(
            "chat/completions", payload, TimeSpan.FromSeconds(timeoutSeconds), ct);

        if (body.Choices is null || body.Choices.Count == 0)
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
