namespace SeraGo.API.Services;

/// <summary>
/// Connection settings for the scraper's database (Neon), from which the
/// admin sync imports jobs. Values come from the gitignored
/// `.env(SeraGo-Scraper)` file (DB_HOST / DB_PORT / DB_NAME / DB_USER /
/// DB_PASSWORD) — never from committed config.
/// </summary>
public sealed class ScraperDbOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 5432;
    public string Database { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Database)
        && !string.IsNullOrWhiteSpace(User);

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Database};Username={User};Password={Password};"
        + "Trust Server Certificate=true";
}
