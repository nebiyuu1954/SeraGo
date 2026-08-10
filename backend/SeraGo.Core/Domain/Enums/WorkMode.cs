namespace SeraGo.Core.Domain.Enums;

/// <summary>
/// Where a job is performed. Mirrors the scraper's <c>job_site</c> values
/// (ONSITE / REMOTE / HYBRID). Used for a talent's remote preference.
/// </summary>
public enum WorkMode
{
    Onsite = 0,
    Remote = 1,
    Hybrid = 2
}
