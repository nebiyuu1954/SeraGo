namespace SeraGo.Core.Domain.Enums;

/// <summary>
/// Profile type that mirrors the user's Identity role.
/// Authorization is enforced via Identity roles (<see cref="Domain.Roles"/>);
/// this enum is a denormalized hint for profile-level reads.
/// </summary>
public enum UserType
{
    Talent = 0,
    Recruiter = 1,
    Admin = 2
}
