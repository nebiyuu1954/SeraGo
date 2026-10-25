using System.Text.RegularExpressions;
using SeraGo.Core.Domain.Entities;

namespace SeraGo.Core.Domain;

/// <summary>
/// Detects cross-site scraper duplicate job postings across Ethiopian job boards
/// (e.g. EthioJobs, HaHuJobs, Afriwork, GeezJobs) where the same opening is listed
/// with minor differences in employer punctuation, acronyms, or title noise.
/// </summary>
public static class JobDeduplicationHelper
{
    private static readonly Regex ParenthesesRegex = new(@"\([^\)]*\)", RegexOptions.Compiled);
    private static readonly Regex CorporateSuffixesRegex = new(
        @"\b(plc|p\.l\.c|llc|ltd|corp|corporation|inc|sc|s\.c|share\s+company|enterprise|co|company|group|holding)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NonAlphanumericRegex = new(@"[^a-z0-9]", RegexOptions.Compiled);
    private static readonly Regex TitleNoiseRegex = new(
        @"\b(re-advertised|readvertised|urgent|urgently|immediate|vacancy|position|hiring|contract|full-time|part-time)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Normalizes a company name for fuzzy duplicate comparison.
    /// Example: "Ethiopian Construction Works Corporation (ECWC)" -> "ethiopianconstructionworks"
    /// </summary>
    public static string NormalizeCompany(string? company)
    {
        if (string.IsNullOrWhiteSpace(company)) return string.Empty;

        var s = company.ToLowerInvariant();
        s = ParenthesesRegex.Replace(s, " ");
        s = CorporateSuffixesRegex.Replace(s, " ");
        s = NonAlphanumericRegex.Replace(s, "");
        return s.Trim();
    }

    /// <summary>
    /// Normalizes a job title for fuzzy duplicate comparison.
    /// Example: "Nurse (Re-advertised)" -> "nurse"
    /// </summary>
    public static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        var s = title.ToLowerInvariant();
        s = ParenthesesRegex.Replace(s, " ");
        s = TitleNoiseRegex.Replace(s, " ");
        s = NonAlphanumericRegex.Replace(s, "");
        return s.Trim();
    }

    /// <summary>
    /// Generates a normalized canonical key for fast dictionary lookup: "company|title".
    /// </summary>
    public static string GenerateCanonicalKey(string? company, string? title)
    {
        var comp = NormalizeCompany(company);
        var t = NormalizeTitle(title);
        if (string.IsNullOrEmpty(comp) || string.IsNullOrEmpty(t)) return string.Empty;
        return $"{comp}|{t}";
    }

    /// <summary>
    /// Determines whether two raw company and title pairs represent the same vacancy.
    /// </summary>
    public static bool AreDuplicate(string? rawCompA, string? rawTitleA, string? rawCompB, string? rawTitleB)
    {
        var compA = NormalizeCompany(rawCompA);
        var compB = NormalizeCompany(rawCompB);
        if (string.IsNullOrEmpty(compA) || string.IsNullOrEmpty(compB)) return false;

        // Company match: exact normalized match OR one contains the other (min 5 chars)
        bool companyMatches = compA == compB ||
            (compA.Length >= 5 && compB.Length >= 5 && (compA.Contains(compB) || compB.Contains(compA)));
        if (!companyMatches) return false;

        var titleA = NormalizeTitle(rawTitleA);
        var titleB = NormalizeTitle(rawTitleB);
        if (string.IsNullOrEmpty(titleA) || string.IsNullOrEmpty(titleB)) return false;

        // Title match: exact normalized match OR one contains the other (min 4 chars)
        bool titleMatches = titleA == titleB ||
            (titleA.Length >= 4 && titleB.Length >= 4 && (titleA.Contains(titleB) || titleB.Contains(titleA)));

        return titleMatches;
    }

    /// <summary>
    /// Determines whether two job listings represent the same vacancy advertised on different platforms.
    /// </summary>
    public static bool AreDuplicateJobs(Job a, Job b)
    {
        if (a.Id == b.Id) return true;
        return AreDuplicate(a.Company, a.Title, b.Company, b.Title);
    }
}
