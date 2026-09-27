namespace SeraGo.Core.Domain;

/// <summary>
/// The roles that exist in SeraGo. Used for seeding, signup role checks,
/// and [Authorize(Roles = ...)] attributes.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Talent = "Talent";

    /// <summary>Every role in the system. Only these can ever be assigned.</summary>
    public static readonly string[] All = [Admin, Talent];

    /// <summary>Roles a user may self-select at registration. Admin is never self-service.</summary>
    public static readonly string[] SelfService = [Talent];
}
