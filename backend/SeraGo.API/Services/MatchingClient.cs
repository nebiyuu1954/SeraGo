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
    /// Posts a JSON body with an explicit Content-Length (via StringContent).
    /// PostAsJsonAsync/JsonContent sends `Transfer-Encoding: chunked` instead,
    /// which Django's dev-server WSGI handler does not decode — it would see
    /// an empty body and reject the call with 400.
    /// </summary>
    private Task<HttpResponseMessage> SendJsonAsync(string path, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return _http.PostAsync(path, content, ct);
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

            await SendJsonAsync("api/matching/webhook/job-published", payload, ct);
            _logger.LogInformation("Notified AI service: job {JobId} published, {Count} eligible talents", jobId, eligibleTalents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify AI service about job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Fetch the stored 0-100 match scores for one talent against a list of
    /// jobs (used to annotate the talent's "For You" feed). Best-effort — on
    /// any failure an empty map is returned so job listings still render.
    /// </summary>
    public async Task<Dictionary<Guid, JobScore>> GetForYouJobScoresAsync(
        string userId, IReadOnlyCollection<Guid> jobIds, CancellationToken ct = default)
    {
        try
        {
            if (jobIds.Count == 0) return new Dictionary<Guid, JobScore>();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

            var payload = new { userId, jobIds = jobIds.Select(j => j.ToString()).ToList() };
            var response = await SendJsonAsync("api/matching/scores/batch", payload, timeoutCts.Token);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ScoreBatchResponse>(JsonOpts, timeoutCts.Token);
            if (result?.Scores is null) return new Dictionary<Guid, JobScore>();

            return result.Scores
                .Where(kv => Guid.TryParse(kv.Key, out _))
                .ToDictionary(kv => Guid.Parse(kv.Key), kv => kv.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch For You scores for {Count} jobs", jobIds.Count);
            return new Dictionary<Guid, JobScore>();
        }
    }

    /// <summary>
    /// Fetch the stored 0-100 scores for a list of applications (used by the
    /// recruiter applications pages). Best-effort.
    /// </summary>
    public async Task<Dictionary<Guid, ApplicationScoreDto>> GetApplicationScoresAsync(
        IReadOnlyCollection<Guid> applicationIds, CancellationToken ct = default)
    {
        try
        {
            if (applicationIds.Count == 0) return new Dictionary<Guid, ApplicationScoreDto>();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

            var payload = new { applicationIds = applicationIds.Select(id => id.ToString()).ToList() };
            var response = await SendJsonAsync("api/matching/application-scores/batch", payload, timeoutCts.Token);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApplicationScoreBatchResponse>(JsonOpts, timeoutCts.Token);
            if (result?.Scores is null) return new Dictionary<Guid, ApplicationScoreDto>();

            return result.Scores
                .Where(kv => Guid.TryParse(kv.Key, out _))
                .ToDictionary(kv => Guid.Parse(kv.Key), kv => kv.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch application scores for {Count} applications", applicationIds.Count);
            return new Dictionary<Guid, ApplicationScoreDto>();
        }
    }

    /// <summary>
    /// Fire-and-forget: notify the AI service that an application was created.
    /// The AI service scores the application (talent snapshot vs job) and
    /// stores the 0-100 score for the recruiter applications page.
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

            await SendJsonAsync("api/matching/webhook/application", payload, ct);
            _logger.LogInformation("Notified AI service: application {ApplicationId} created", applicationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify AI service about application {ApplicationId}", applicationId);
        }
    }

    /// <summary>
    /// Synchronous: score one talent against a list of jobs right now — the
    /// For You page's "Run AI matching" button. The AI service computes and
    /// stores a 0-100 score per job and returns how many were scored.
    /// </summary>
    public async Task<ForYouRefreshResult> ScoreForYouJobsAsync(
        Dictionary<string, object?> talentProfile,
        List<Dictionary<string, object?>> jobs,
        CancellationToken ct = default)
    {
        try
        {
            if (jobs.Count == 0) return new ForYouRefreshResult(true, "ok", 0, 0, null);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            // Scoring each job (embedding + components) takes a few seconds for
            // a full feed, so this is deliberately more generous than the reads.
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(90));

            var payload = new { talent = talentProfile, jobs };
            var response = await SendJsonAsync("api/matching/refresh-for-you", payload, timeoutCts.Token);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ForYouRefreshResponse>(JsonOpts, timeoutCts.Token);
            return new ForYouRefreshResult(
                true,
                result?.Status ?? "ok",
                result?.Scored ?? 0,
                result?.Cached ?? 0,
                result?.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh For You scores for {Count} jobs", jobs.Count);
            return new ForYouRefreshResult(false, "unavailable", 0, 0, null);
        }
    }

    /// <summary>
    /// Synchronous: score a batch of stored applications now — the recruiter's
    /// "Run AI matching" button on the applications page. Each entry carries
    /// the application's apply-time profile snapshot + the job; the AI service
    /// scores and stores each one, returning how many were scored/reused/failed.
    /// </summary>
    public async Task<RescoreApplicationsResult> RescoreApplicationsAsync(
        List<Dictionary<string, object?>> applications,
        CancellationToken ct = default)
    {
        try
        {
            if (applications.Count == 0)
                return new RescoreApplicationsResult(true, "ok", 0, 0, 0, null);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(90));

            var payload = new { applications };
            var response = await SendJsonAsync("api/matching/applications/rescore", payload, timeoutCts.Token);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<RescoreApplicationsResponse>(JsonOpts, timeoutCts.Token);
            return new RescoreApplicationsResult(
                true,
                result?.Status ?? "ok",
                result?.Scored ?? 0,
                result?.Cached ?? 0,
                result?.Failed?.Count ?? 0,
                null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to rescore {Count} applications", applications.Count);
            return new RescoreApplicationsResult(false, "unavailable", 0, 0, 0, null);
        }
    }

    // ── DTOs ────────────────────────────────────────────────────────────

    /// <summary>Result of a refresh run: freshly-scored and cache-reused counts.</summary>
    public sealed record ForYouRefreshResult(bool Succeeded, string Status, int Scored, int Cached, string? Reason);

    /// <summary>Result of an application rescore run.</summary>
    public sealed record RescoreApplicationsResult(
        bool Succeeded, string Status, int Scored, int Cached, int Failed, string? Reason);

    private sealed class RescoreApplicationsResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("scored")]
        public int? Scored { get; set; }

        [JsonPropertyName("cached")]
        public int? Cached { get; set; }

        [JsonPropertyName("failed")]
        public List<object>? Failed { get; set; }
    }

    private sealed class ForYouRefreshResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("scored")]
        public int? Scored { get; set; }

        [JsonPropertyName("cached")]
        public int? Cached { get; set; }
    }

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

        /// <summary>The talent's "For You" sector ids (UserSettings.forYou.sectorIds).</summary>
        public List<string> PreferredSectorIds { get; set; } = [];

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
            ["preferredSectorIds"] = PreferredSectorIds,
        };
    }

    /// <summary>A stored talent-job match score (0-100).</summary>
    public sealed class JobScore
    {
        [JsonPropertyName("score")]
        public double? Score { get; set; }

        [JsonPropertyName("matched")]
        public List<string>? Matched { get; set; }

        [JsonPropertyName("missing")]
        public List<string>? Missing { get; set; }
    }

    /// <summary>A stored application score (0-100).</summary>
    public sealed class ApplicationScoreDto
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("score")]
        public double? Score { get; set; }

        [JsonPropertyName("matched")]
        public List<string>? Matched { get; set; }

        [JsonPropertyName("missing")]
        public List<string>? Missing { get; set; }
    }

    private sealed class ScoreBatchResponse
    {
        [JsonPropertyName("scores")]
        public Dictionary<string, JobScore>? Scores { get; set; }
    }

    private sealed class ApplicationScoreBatchResponse
    {
        [JsonPropertyName("scores")]
        public Dictionary<string, ApplicationScoreDto>? Scores { get; set; }
    }
}
