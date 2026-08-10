namespace SeraGo.Core.Domain.Enums;

/// <summary>
/// How soon a talent can start a new role.
/// </summary>
public enum Availability
{
    Immediate = 0,
    WithinTwoWeeks = 1,
    WithinOneMonth = 2,
    MoreThanOneMonth = 3
}
