using Aufy.Core.Endpoints;
using System.ComponentModel.DataAnnotations;

namespace SeraGo.API.Auth;

/// <summary>
/// Custom signup model: adds profile fields + role selection to Aufy's default
/// email/password signup. See <see cref="SeraGoSignUpExtension"/> for role handling.
/// </summary>
public class SeraGoSignUpRequest : SignUpRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Retained for backwards compatibility and ignored — every self-service
    /// signup now creates a Talent account.
    /// </summary>
    public string? Role { get; set; }
}
