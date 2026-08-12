using SeraGo.Core.Domain.Enums;

namespace SeraGo.Core.Domain;

/// <summary>
/// Parses the API's accepted <c>JobType</c> vocabulary into the .NET
/// <see cref="JobType"/> enum. Accepts the classic scraper-style values
/// (FULL_TIME, PART_TIME, ...) and the enum names (FullTime, ...),
/// case-insensitively.
/// </summary>
public static class JobTypes
{
    /// <summary>Scraper DB value → enum. Accepts both the scraper strings and the enum names.</summary>
    public static readonly IReadOnlyDictionary<string, JobType> ByScraperValue =
        new Dictionary<string, JobType>(StringComparer.OrdinalIgnoreCase)
        {
            ["FULL_TIME"] = JobType.FullTime,
            ["PART_TIME"] = JobType.PartTime,
            ["CONTRACT"] = JobType.Contract,
            ["CONTRACTUAL"] = JobType.Contractual,
            ["REMOTE"] = JobType.Remote,
            ["INTERNSHIP"] = JobType.Internship,
            ["FREELANCE"] = JobType.Freelance,
            ["TEMPORARY"] = JobType.Temporary,
            ["OTHER"] = JobType.Other,
        };

    /// <summary>
    /// Parses a value into a <see cref="JobType"/>. Accepts the scraper's DB
    /// values ("FULL_TIME") and the enum names ("FullTime"), case-insensitively.
    /// Numeric strings and unknown values return false (with Other as fallback).
    /// </summary>
    public static bool TryParse(string? value, out JobType type)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (ByScraperValue.TryGetValue(value, out type))
            {
                return true;
            }

            // Enum names ("FullTime") are accepted too, but numeric strings
            // ("3") must not silently become an enum value.
            if (!int.TryParse(value, out _)
                && Enum.TryParse(value, ignoreCase: true, out JobType parsed)
                && Enum.IsDefined(parsed))
            {
                type = parsed;
                return true;
            }
        }

        type = JobType.Other;
        return false;
    }

    /// <summary>Both accepted vocabularies, for validation error messages.</summary>
    public static string ValidValuesDescription =>
        string.Join(", ", ByScraperValue.Keys) + " (or " + string.Join(", ", Enum.GetNames(typeof(JobType))) + ")";
}
