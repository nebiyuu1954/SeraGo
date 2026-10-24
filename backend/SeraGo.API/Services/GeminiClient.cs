using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeraGo.API.Services;

/// <summary>
/// HTTP client for Google Gemini Generative Language API.
/// Uses gemini-1.5-flash / gemini-2.0-flash for high-speed, multilingual
/// Ethiopian job classification and reasoning.
/// </summary>
public sealed class GeminiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<GeminiClient> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public GeminiClient(IHttpClientFactory factory, IConfiguration config, ILogger<GeminiClient> logger)
    {
        _logger = logger;
        _apiKey = config["GEMINI_API_KEY"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? config["GOOGLE_API_KEY"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
            ?? string.Empty;

        _model = config["GEMINI_MODEL"]
            ?? Environment.GetEnvironmentVariable("GEMINI_MODEL")
            ?? "gemini-2.0-flash";

        var http = factory.CreateClient($"Gemini-{Guid.NewGuid():N}");
        http.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        http.Timeout = TimeSpan.FromSeconds(45);
        _http = http;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<TResponse> GenerateJsonAsync<TResponse>(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("GEMINI_API_KEY is not configured.");
        }

        var fullPrompt = $"{systemPrompt}\n\nTask:\n{userPrompt}";

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new { text = fullPrompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.0,
                responseMimeType = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var endpoint = $"models/{_model}:generateContent?key={_apiKey}";
        using var response = await _http.PostAsync(endpoint, content, ct);

        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini API call failed: {StatusCode} body={Body}", response.StatusCode, responseText);
            throw new HttpRequestException($"Gemini returned {response.StatusCode}: {responseText}");
        }

        using var doc = JsonDocument.Parse(responseText);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini returned no candidates in response.");
        }

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var contentObj) ||
            !contentObj.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini response missing content parts.");
        }

        var rawJsonText = parts[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(rawJsonText))
        {
            throw new InvalidOperationException("Gemini returned empty text response.");
        }

        return JsonSerializer.Deserialize<TResponse>(rawJsonText, JsonOpts)
            ?? throw new InvalidOperationException("Failed to deserialize JSON from Gemini.");
    }
}
