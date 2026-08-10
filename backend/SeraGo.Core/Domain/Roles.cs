namespace SeraGo.Core.Domain;

/// <summary>
/// The three roles that exist in SeraGo. Used for seeding, signup role checks,
/// and [Authorize(Roles = ...)] attributes.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Talent = "Talent";
    public const string Recruiter = "Recruiter";

    /// <summary>Every role in the system. Only these can ever be assigned.</summary>
    public static readonly string[] All = [Admin, Talent, Recruiter];

    /// <summary>Roles a user may self-select at registration. Admin is never self-service.</summary>
    public static readonly string[] SelfService = [Talent, Recruiter];
}
