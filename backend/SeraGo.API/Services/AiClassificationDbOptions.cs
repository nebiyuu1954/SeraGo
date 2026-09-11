namespace SeraGo.API.Services;

/// <summary>
/// Connection settings for the SeraGo-AI service database — its own Neon
/// database, which owns the classify audit tables
/// (<c>ai_service_aiclassificationlog</c> + <c>ai_service_aiclassificationraw</c>).
///
/// .NET never writes to those tables; the admin stats endpoint only reads
/// aggregates from them. Values come from the gitignored <c>.env(SeraGo-AI)</c>
/// file (<c>AI_DB_HOST</c> / <c>AI_DB_PORT</c> / <c>AI_DB_NAME</c> /
/// <c>AI_DB_USER</c> / <c>AI_DB_PASSWORD</c>) or a single full connection
/// string in <c>AI_DB_CONNECTION</c> (postgresql:// URI or Npgsql key=value).
/// </summary>
public sealed class AiClassificationDbOptions
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
        + "SSL Mode=Require;Trust Server Certificate=true";

    public static AiClassificationDbOptions FromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable("AI_DB_CONNECTION");
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return FromConnectionString(raw.Trim());
        }

        return new AiClassificationDbOptions
        {
            Host = Environment.GetEnvironmentVariable("AI_DB_HOST") ?? string.Empty,
            Port = int.TryParse(Environment.GetEnvironmentVariable("AI_DB_PORT"), out var port) ? port : 5432,
            Database = Environment.GetEnvironmentVariable("AI_DB_NAME") ?? string.Empty,
            User = Environment.GetEnvironmentVariable("AI_DB_USER") ?? string.Empty,
            Password = Environment.GetEnvironmentVariable("AI_DB_PASSWORD") ?? string.Empty,
        };
    }

    /// <summary>Accepts a postgresql:// URI or an Npgsql key=value string.</summary>
    private static AiClassificationDbOptions FromConnectionString(string value)
    {
        if (value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(value);
            var userInfo = uri.UserInfo.Split(':', 2);
            return new AiClassificationDbOptions
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
                Database = uri.AbsolutePath.TrimStart('/'),
                User = Uri.UnescapeDataString(userInfo[0]),
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            };
        }

        var csb = new Npgsql.NpgsqlConnectionStringBuilder(value);
        return new AiClassificationDbOptions
        {
            Host = csb.Host ?? string.Empty,
            Port = csb.Port,
            Database = csb.Database ?? string.Empty,
            User = csb.Username ?? string.Empty,
            Password = csb.Password ?? string.Empty,
        };
    }
}
