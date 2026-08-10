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
    /// Must be "Talent" or "Recruiter" (validated server-side against the whitelist).
    /// No default value on purpose: omitting it must fail, not silently become Talent.
    /// "Admin" is never self-service.
    /// </summary>
    [Required]
    public string? Role { get; set; }
}
