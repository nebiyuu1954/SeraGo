using System.ComponentModel.DataAnnotations;
using Aufy.Core.Endpoints;

namespace SeraGo.API.Auth;

/// <summary>
/// Body of POST /api/auth/signup/external — the role + display-name step a
/// brand-new Google user completes after the OAuth handshake. Email and the
/// provider identity come from Google's claims, never from this payload.
///
/// Inherits Aufy's <see cref="SignUpExternalRequest"/> because Aufy 1.0.0's
/// <see cref="ISignUpExternalEndpointEvents{TUser,TModel}"/> constrains TModel
/// to it.
/// </summary>
public class SeraGoSignUpExternalRequest : SignUpExternalRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Must be "Talent" or "Recruiter" (validated server-side against the
    /// shared whitelist). No default on purpose — omitting it must fail.
    /// </summary>
    [Required]
    public string? Role { get; set; }
}
