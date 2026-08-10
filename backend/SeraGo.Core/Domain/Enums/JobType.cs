namespace SeraGo.Core.Domain.Enums;

/// <summary>
/// Employment type. Mirrors the scraper's <c>JobType</c> choices exactly
/// (FULL_TIME / PART_TIME / ...) so talent preferences and scraped job
/// postings compare without translation.
/// </summary>
public enum JobType
{
    FullTime = 0,
    PartTime = 1,
    Contract = 2,
    Contractual = 3,
    Remote = 4,
    Internship = 5,
    Freelance = 6,
    Temporary = 7,
    Other = 8
}
