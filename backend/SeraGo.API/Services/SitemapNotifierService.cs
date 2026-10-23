using Hangfire;

namespace SeraGo.API.Services;

/// <summary>
/// Hangfire background service that pings search engine indexing protocols (IndexNow)
/// when new jobs are published or synced, notifying Bing, Yandex, and participating
/// crawlers to index the new URLs immediately.
/// </summary>
public class SitemapNotifierService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SitemapNotifierService> _logger;
    private readonly IConfiguration _config;

    public SitemapNotifierService(
        IHttpClientFactory httpClientFactory,
        ILogger<SitemapNotifierService> logger,
        IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Background job invoked when new jobs are published or imported.
    /// Submits the list of URLs to the IndexNow API protocol.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task NotifySearchEnginesAsync(List<string> newJobUrls)
    {
        if (newJobUrls == null || newJobUrls.Count == 0) return;

        var host = _config["SEO:Host"] ?? _config["Frontend:Host"] ?? "serago.pro.et";
        var indexNowKey = _config["SEO:IndexNowKey"] ?? _config["INDEXNOW_KEY"];

        if (string.IsNullOrWhiteSpace(indexNowKey))
        {
            _logger.LogInformation("IndexNow API key not configured (SEO:IndexNowKey / INDEXNOW_KEY); skipping search engine notification for {Count} jobs", newJobUrls.Count);
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                host,
                key = indexNowKey,
                keyLocation = $"https://{host}/{indexNowKey}.txt",
                urlList = newJobUrls
            };

            var response = await client.PostAsJsonAsync("https://api.indexnow.org/indexnow", payload);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully submitted {Count} job URLs to IndexNow (host: {Host})", newJobUrls.Count, host);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("IndexNow ping returned HTTP {StatusCode}: {Response}", response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit {Count} job URLs to IndexNow search engine service", newJobUrls.Count);
            throw; // Hangfire AutomaticRetry will retry
        }
    }
}
