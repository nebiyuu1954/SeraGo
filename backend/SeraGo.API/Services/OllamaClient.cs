using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeraGo.API.Services;

/// <summary>
/// HTTP client for local Ollama instances (e.g. running llama3.2 or mistral).
/// Acts as an offline/on-prem fallback when cloud AI APIs (Gemini/Groq) are unreachable.
/// </summary>
public sealed class OllamaClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OllamaClient> _logger;
    private readonly string _model;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public OllamaClient(IHttpClientFactory factory, IConfiguration config, ILogger<OllamaClient> logger)
    {
        _logger = logger;
        var baseUrl = config["OLLAMA_BASE_URL"]
            ?? Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")
            ?? "http://127.0.0.1:11434";

        _model = config["OLLAMA_MODEL"]
            ?? Environment.GetEnvironmentVariable("OLLAMA_MODEL")
            ?? "llama3.2";

        var http = factory.CreateClient($"Ollama-{Guid.NewGuid():N}");
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.Timeout = TimeSpan.FromSeconds(60);
        _http = http;
    }

    public async Task<TResponse> ChatJsonAsync<TResponse>(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default)
    {
        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            format = "json",
            stream = false
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync("api/chat", content, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama call failed: {StatusCode} body={Body}", response.StatusCode, responseText);
            throw new HttpRequestException($"Ollama returned {response.StatusCode}: {responseText}");
        }

        using var doc = JsonDocument.Parse(responseText);
        if (!doc.RootElement.TryGetProperty("message", out var messageObj) ||
            !messageObj.TryGetProperty("content", out var contentProp))
        {
            throw new InvalidOperationException("Ollama response missing message.content.");
        }

        var rawJsonText = contentProp.GetString();
        if (string.IsNullOrWhiteSpace(rawJsonText))
        {
            throw new InvalidOperationException("Ollama returned empty content.");
        }

        return JsonSerializer.Deserialize<TResponse>(rawJsonText, JsonOpts)
            ?? throw new InvalidOperationException("Failed to deserialize JSON from Ollama.");
    }
}
