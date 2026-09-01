using Aufy.Core;
using SeraGo.Core.Domain.Enums;

namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// SeraGo user. Built on ASP.NET Core Identity (via Aufy) so roles,
/// password hashing, lockout and email confirmation come for free.
/// </summary>
public class ApplicationUser : AufyUser
{
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // Common profile fields — every role has these.
    public string AvatarUrl { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public UserType UserType { get; set; } = UserType.Talent;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>1:1 Talent profile — populated only for Talent users.</summary>
    public TalentProfile? TalentProfile { get; set; }

    /// <summary>1:1 Recruiter profile — populated only for Recruiter users.</summary>
    public RecruiterProfile? RecruiterProfile { get; set; }

    /// <summary>1:1 user settings — every role gets a row.</summary>
    public UserSettings? UserSettings { get; set; }
}
