using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeraGo.API.Services;

/// <summary>
/// HTTP client for calling the SeraGo-AI matching engine webhooks.
/// All calls are fire-and-forget (best-effort) — the matching engine is
/// optional and failures should never block the main request.
///
/// Configure via env vars:
///   MATCHING_API_URL  — base URL of the AI service (default: http://localhost:8001)
///   MATCHING_API_KEY  — shared secret (X-Api-Key header)
/// </summary>
public class MatchingClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MatchingClient> _logger;
    private readonly string _apiKey;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public MatchingClient(HttpClient http, ILogger<MatchingClient> logger, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["MATCHING_API_KEY"] ?? Environment.GetEnvironmentVariable("MATCHING_API_KEY") ?? "";

        var baseUrl = config["MATCHING_API_URL"] ?? Environment.GetEnvironmentVariable("MATCHING_API_URL") ?? "http://localhost:8001";
        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
    }

    /// <summary>
    /// Fire-and-forget: notify the AI service that a job was published.
    /// Scores the job against eligible talents in the background.
    /// </summary>
    public async Task NotifyJobPublishedAsync(
        Guid jobId, string title, string? description, string? company,
        Guid? sectorId, string? sectorName, string? experienceLevel,
        string? jobType, string? workMode, string? skills,
        int? experienceMinYears, int? experienceMaxYears,
        List<TalentProfileForMatching> eligibleTalents,
        CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                job = new
                {
                    jobId = jobId.ToString(),
                    title,
                    description,
                    company,
                    sectorId = sectorId?.ToString(),
                    sectorName,
                    experienceLevel,
                    jobType,
                    workMode,
                    skills,
                    experienceMinYears,
                    experienceMaxYears,
                },
                eligibleTalents = eligibleTalents.Select(t => t.ToDict()).ToList(),
            };

            await _http.PostAsJsonAsync("api/matching/webhook/job-published", payload, JsonOpts, ct);
            _logger.LogInformation("Notified AI service: job {JobId} published, {Count} eligible talents", jobId, eligibleTalents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify AI service about job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Fire-and-forget: notify the AI service that a talent logged in.
    /// Triggers background scoring for un-scored jobs.
    /// </summary>
    public async Task NotifyTalentLoginAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            var payload = new { userId };
            await _http.PostAsJsonAsync("api/matching/webhook/talent-login", payload, JsonOpts, ct);
            _logger.LogInformation("Notified AI service: talent {UserId} logged in", userId[..8]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify AI service about talent login {UserId}", userId[..8]);
        }
    }

    /// <summary>
    /// Fire-and-forget: store an application for deferred scoring.
    /// </summary>
    public async Task NotifyApplicationCreatedAsync(
        Guid applicationId, string talentUserId, Guid jobId,
        Dictionary<string, object?> talentProfile, Dictionary<string, object?> jobData,
        CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                applicationId = applicationId.ToString(),
                talentProfile,
                job = jobData,
            };

            await _http.PostAsJsonAsync("api/matching/webhook/application", payload, JsonOpts, ct);
            _logger.LogInformation("Notified AI service: application {ApplicationId} created", applicationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify AI service about application {ApplicationId}", applicationId);
        }
    }

    /// <summary>
    /// Fire-and-forget: notify the AI service that a talent updated their profile.
    /// Invalidates stale scores if scoring-relevant fields changed.
    /// </summary>
    public async Task NotifyTalentUpdatedAsync(
        string userId,
        Dictionary<string, object?> currentProfile,
        Dictionary<string, object?>? previousProfile = null,
        CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                talentProfile = currentProfile,
                previousProfile,
            };

            await _http.PostAsJsonAsync("api/matching/webhook/talent-updated", payload, JsonOpts, ct);
            _logger.LogInformation("Notified AI service: talent {UserId} profile updated", userId[..8]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify AI service about talent profile update {UserId}", userId[..8]);
        }
    }

    /// <summary>
    /// Classify a job title into a canonical sector.
    /// Returns (sectorId, sectorName) or null if unclassifiable.
    /// </summary>
    public async Task<(Guid? sectorId, string? sectorName)?> ClassifySectorAsync(
        string title, string? rawSector = null, string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            var payload = new { title, rawSector, description };
            var response = await _http.PostAsJsonAsync("api/matching/classify-sector", payload, JsonOpts, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ClassifySectorResponse>(JsonOpts, ct);
            if (result?.SectorId is null) return null;

            return (Guid.Parse(result.SectorId), result.SectorName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to classify sector for title: {Title}", title);
            return null;
        }
    }

    // ── DTOs ────────────────────────────────────────────────────────────

    public class TalentProfileForMatching
    {
        public string UserId { get; set; } = "";
        public string? Headline { get; set; }
        public string? About { get; set; }
        public List<string> Skills { get; set; } = [];
        public string? ExperienceLevel { get; set; }
        public int? YearsOfExperience { get; set; }
        public List<string> DesiredRoles { get; set; } = [];
        public List<string> DesiredJobTypes { get; set; } = [];
        public string? CurrentIndustry { get; set; }
        public string? CurrentProfession { get; set; }
        public string? WorkMode { get; set; }
        public List<string> PreferredLocations { get; set; } = [];
        public string? WorkExperience { get; set; }
        public string? EducationHistory { get; set; }

        public Dictionary<string, object?> ToDict() => new()
        {
            ["userId"] = UserId,
            ["headline"] = Headline,
            ["about"] = About,
            ["skills"] = Skills,
            ["experienceLevel"] = ExperienceLevel,
            ["yearsOfExperience"] = YearsOfExperience,
            ["desiredRoles"] = DesiredRoles,
            ["desiredJobTypes"] = DesiredJobTypes,
            ["currentIndustry"] = CurrentIndustry,
            ["currentProfession"] = CurrentProfession,
            ["workMode"] = WorkMode,
            ["preferredLocations"] = PreferredLocations,
            ["workExperience"] = WorkExperience,
            ["educationHistory"] = EducationHistory,
        };
    }

    private class ClassifySectorResponse
    {
        [JsonPropertyName("sectorId")]
        public string? SectorId { get; set; }

        [JsonPropertyName("sectorName")]
        public string? SectorName { get; set; }
    }
}
