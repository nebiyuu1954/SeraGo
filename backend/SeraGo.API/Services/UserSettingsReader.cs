using System.Text.Json;

namespace SeraGo.API.Services;

/// <summary>
/// Reads SeraGo settings JSON blobs (UserSettings.Settings) for values that
/// backend services depend on. The frontend owns the shape; these helpers
/// parse defensively so a missing or malformed category never throws.
/// </summary>
public static class UserSettingsReader
{
    /// <summary>
    /// Extracts the "For you" sector ids from the settings blob, e.g.
    /// <c>{ "forYou": { "sectorIds": ["&lt;guid&gt;", ...] } }</c>.
    /// Returns an empty list when absent or malformed.
    /// </summary>
    public static List<Guid> GetForYouSectorIds(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson)) return [];

        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            if (!doc.RootElement.TryGetProperty("forYou", out var forYou)) return [];
            if (!forYou.TryGetProperty("sectorIds", out var sectorIds)) return [];

            var ids = new List<Guid>();
            foreach (var element in sectorIds.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(element.GetString(), out var id))
                {
                    ids.Add(id);
                }
            }
            return ids;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
