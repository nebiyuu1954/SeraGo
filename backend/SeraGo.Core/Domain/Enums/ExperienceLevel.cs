namespace SeraGo.Core.Domain.Enums;

/// <summary>
/// Career seniority band. Mirrors the scraper's <c>experience_level</c>
/// values (ENTRY / JUNIOR / SENIOR / ...) used on job postings.
/// </summary>
public enum ExperienceLevel
{
    Entry = 0,
    Junior = 1,
    Mid = 2,
    Senior = 3,
    Lead = 4
}
