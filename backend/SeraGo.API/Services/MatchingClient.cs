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

            // Generous on purpose: this is a stored-score READ, but the AI service's
            // Neon DB autosuspends after ~5 min idle, so the first read after an idle
            // period pays a wake-up penalty. Measured: ~1.5-2.3s warm, 17.1s cold.
            // At 3s the call was cancelled and the feed SILENTLY degraded to null
            // scores + newest-first ordering (SocketException 995 in the logs).
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

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

            // Same reasoning as GetForYouJobScoresAsync above: this looks like a
            // cheap stored-score read, but the AI service's Neon DB autosuspends
            // after ~5 min idle, so a cold read runs well past 3s and would have
            // been cancelled — silently dropping the score badges.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

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

    /// <summary>
    /// Synchronous: ask the AI service to read a resume PDF and return the
    /// profile fields it could find. The URL must be a short-lived presigned
    /// R2 URL — resumes live in a private bucket and the AI service holds no
    /// R2 credentials of its own.
    ///
    /// Best-effort, like every other method here: returns null when the service
    /// is unreachable, so the caller can report "we couldn't read your resume"
    /// instead of an error the user is expected to fix. A scanned (image-only)
    /// PDF is NOT a failure — it returns successfully with zero characters and
    /// an empty profile.
    /// </summary>
    public async Task<ResumeParseResult?> ParseResumeAsync(
        string presignedResumeUrl, CancellationToken ct = default)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            // 30s download + extraction on the AI side; 60s matches the
            // service's own budget for the whole request.
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

            var payload = new { resumeUrl = presignedResumeUrl };
            var response = await SendJsonAsync("api/matching/parse-resume", payload, timeoutCts.Token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ResumeParseResult>(JsonOpts, timeoutCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse resume via the AI service");
            return null;
        }
    }

    /// <summary>
    /// Result of POST /api/matching/parse-resume. Property names carry explicit
    /// [JsonPropertyName] values so the wire shape is identical whether or not
    /// the host applies a JSON naming policy — the frontend reads these as-is.
    /// </summary>
    public sealed class ResumeParseResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("charsExtracted")]
        public int CharsExtracted { get; set; }

        [JsonPropertyName("pagesRead")]
        public int PagesRead { get; set; }

        [JsonPropertyName("fieldsFound")]
        public int FieldsFound { get; set; }

        [JsonPropertyName("profile")]
        public ResumeProfile? Profile { get; set; }
    }

    /// <summary>
    /// Extracted resume fields. Key names deliberately mirror the frontend's
    /// form shapes (WorkExperienceEntry / EducationEntry in ProfileForm.tsx) so
    /// the values drop straight into the form with no mapping layer.
    /// </summary>
    public sealed class ResumeProfile
    {
        [JsonPropertyName("headline")]
        public string? Headline { get; set; }

        [JsonPropertyName("about")]
        public string? About { get; set; }

        [JsonPropertyName("skills")]
        public List<string>? Skills { get; set; }

        [JsonPropertyName("experience")]
        public List<ResumeWorkExperience>? Experience { get; set; }

        [JsonPropertyName("education")]
        public List<ResumeEducation>? Education { get; set; }

        [JsonPropertyName("currentProfession")]
        public string? CurrentProfession { get; set; }

        [JsonPropertyName("currentIndustry")]
        public string? CurrentIndustry { get; set; }

        /// <summary>One of Entry | Junior | Mid | Senior | Lead.</summary>
        [JsonPropertyName("experienceLevel")]
        public string? ExperienceLevel { get; set; }

        [JsonPropertyName("yearsOfExperience")]
        public int? YearsOfExperience { get; set; }
    }

    /// <summary>One extracted role. Dates are YYYY-MM-DD (the form uses date inputs).</summary>
    public sealed class ResumeWorkExperience
    {
        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("startDate")]
        public string? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string? EndDate { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    /// <summary>One extracted education entry. `level` is from the form's own list.</summary>
    public sealed class ResumeEducation
    {
        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("institution")]
        public string? Institution { get; set; }

        [JsonPropertyName("degree")]
        public string? Degree { get; set; }

        [JsonPropertyName("gpa")]
        public string? Gpa { get; set; }

        [JsonPropertyName("startYear")]
        public string? StartYear { get; set; }

        [JsonPropertyName("endYear")]
        public string? EndYear { get; set; }
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
