using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SeraGo.API.Services;

/// <summary>
/// Typed HTTP client for the SeraGo-AI classify API (POST /api/ai/classify).
///
/// Configured in Program.cs from AI_SERVICE_URL + MATCHING_API_KEY. When the
/// AI service is unreachable or returns a non-2xx, an AiClassificationException
/// is thrown so callers can treat the affected jobs as "unclassified" instead
/// of failing the whole sync/classify flow.
///
/// Request: one-to-many jobs. Response: per-job classification results.
/// </summary>
public sealed class AiClassificationClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AiClassificationClient> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public AiClassificationClient(
        IHttpClientFactory factory,
        IOptions<AiClassificationOptions> options,
        ILogger<AiClassificationClient> logger)
    {
        _logger = logger;
        _apiKey = options.Value.ApiKey;
        _baseUrl = options.Value.BaseUrl.TrimEnd('/') + "/";

        var http = factory.CreateClient("SeraGoAiClassify");
        http.BaseAddress = new Uri(_baseUrl);
        http.Timeout = TimeSpan.FromSeconds(600);
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            http.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
        }

        _http = http;
    }

    /// <summary>
    /// Classify one-to-many jobs. Each job is classified independently; a
    /// non-2xx from the AI service surfaces as an AiClassificationException
    /// (best-effort at the HTTP level). Per-job failures inside the AI service
    /// are returned as `error` on the individual result — they do not throw.
    /// </summary>
    public async Task<IReadOnlyList<AiClassificationResult>> ClassifyAsync(
        IReadOnlyList<AiClassificationRequestJob> jobs, CancellationToken ct = default)
    {
        var requestPayload = new AiClassificationRequest(jobs.ToList());

        try
        {
            // NOTE: we serialize manually and send via StringContent instead of
            // PostAsJsonAsync. JsonContent reports an unknown length, so
            // HttpClient sends the body with Transfer-Encoding: chunked — which
            // Django's wsgiref dev server cannot decode (request.body comes
            // back empty and every classify call fails with 400). StringContent
            // buffers the JSON, sets Content-Length, and avoids chunked.
            var json = JsonSerializer.Serialize(requestPayload, JsonOpts);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(
                "api/ai/classify",
                content,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "SeraGo-AI classify returned {StatusCode} for {JobCount} jobs: {Body}",
                    response.StatusCode, jobs.Count, Truncate(errorBody, 1000));
                throw new AiClassificationException(
                    $"SeraGo-AI classify returned {response.StatusCode}. Body: {errorBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var envelope = JsonSerializer.Deserialize<AiClassificationEnvelope>(responseBody, JsonOpts)
                ?? throw new AiClassificationException("SeraGo-AI returned an empty classify response.");

            return envelope.Results;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("SeraGo-AI classify timed out for {JobCount} jobs", jobs.Count);
            throw new AiClassificationException("SeraGo-AI classify timed out.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "SeraGo-AI returned invalid JSON for classify.");
            throw new AiClassificationException("SeraGo-AI returned invalid JSON: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Fetch the AI classification log details (reasoning, error) from the SeraGo-AI service.
    /// </summary>
    public async Task<AiLogResponse?> GetClassificationLogAsync(Guid logId, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync($"api/ai/classify/log/{logId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<AiLogResponse>(responseBody, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch classification log {LogId} from SeraGo-AI.", logId);
            return null;
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}

// ── Request ───────────────────────────────────────────────────────────

public sealed record AiClassificationRequest(
    [property: JsonPropertyName("jobs")]
    List<AiClassificationRequestJob> Jobs);

public sealed record AiClassificationRequestJob(
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("sourceSectors")] List<string> SourceSectors,
    [property: JsonPropertyName("description")] string Description,
    /// <summary>The sector the job currently has, sent so the AI service can record the 'before' state.</summary>
    [property: JsonPropertyName("extraOriginal")]
    Dictionary<string, string>? ExtraOriginal = null);

// ── Response ──────────────────────────────────────────────────────────

public sealed record AiClassificationEnvelope(
    [property: JsonPropertyName("results")]
    List<AiClassificationResult> Results);

public sealed record AiClassificationResult(
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("sectorId")] string? SectorId,
    [property: JsonPropertyName("sectorName")] string? SectorName,
    [property: JsonPropertyName("sectorSlug")] string? SectorSlug,
    [property: JsonPropertyName("subSectorName")] string? SubSectorName,
    [property: JsonPropertyName("alias")] string? Alias,
    [property: JsonPropertyName("confidence")] double? Confidence,
    [property: JsonPropertyName("reasoning")] string? Reasoning,
    [property: JsonPropertyName("uncategorized")] bool Uncategorized,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("suggestedSectors")] List<SuggestedSector>? SuggestedSectors = null,
    /// <summary>
    /// Id of the AiClassificationLog row created for this attempt on the Django side.
    /// .NET stores this on Jobs.ClassificationId so it can point at the latest classify record.
    /// </summary>
    [property: JsonPropertyName("logId")]
    Guid? LogId = null);

public sealed record SuggestedSector(
    [property: JsonPropertyName("sectorId")] string SectorId,
    [property: JsonPropertyName("sectorName")] string SectorName,
    [property: JsonPropertyName("sectorSlug")] string SectorSlug);

public sealed record AiLogResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("jobTitle")] string JobTitle,
    [property: JsonPropertyName("reasoning")] string? Reasoning,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("uncategorized")] bool Uncategorized,
    [property: JsonPropertyName("sectorSlug")] string? SectorSlug,
    [property: JsonPropertyName("createdAt")] string CreatedAt);


// ── Config ────────────────────────────────────────────────────────────

public sealed class AiClassificationOptions
{
    /// <summary>Base URL of the SeraGo-AI service, e.g. http://localhost:8001</summary>
    public string BaseUrl { get; set; } = "http://localhost:8001";

    /// <summary>Shared API key used for X-Api-Key auth (same value as the Django MATCHING_API_KEY).</summary>
    public string ApiKey { get; set; } = string.Empty;
}

public static class AiClassificationOptionsSetup
{
    public static AiClassificationOptions FromEnvironment()
    {
        var baseUrl = Environment.GetEnvironmentVariable("AI_SERVICE_URL")
            ?? Environment.GetEnvironmentVariable("SERAGO_AI_URL")
            // 127.0.0.1 instead of localhost: on Windows, localhost resolves to
            // ::1 first, and Django's dev server binds IPv4 only — the failed
            // IPv6 connect adds ~2s of Happy-Eyeballs delay to every call.
            ?? "http://127.0.0.1:8001";

        var apiKey = Environment.GetEnvironmentVariable("MATCHING_API_KEY")
            ?? Environment.GetEnvironmentVariable("AI_SERVICE_API_KEY")
            ?? string.Empty;

        return new AiClassificationOptions
        {
            BaseUrl = baseUrl.TrimEnd('/'),
            ApiKey = apiKey,
        };
    }
}

/// <summary>
/// Wraps a failure talking to the SeraGo-AI classify service (network, timeout,
/// non-2xx, bad JSON). Callers catch this to fall back to "no classification"
/// (and, for the sync flow, to the local SubSector fallback) rather than
/// crashing the whole sync/classify flow.
/// </summary>
public sealed class AiClassificationException : Exception
{
    public AiClassificationException(string message) : base(message) { }
    public AiClassificationException(string message, Exception inner) : base(message, inner) { }
}
