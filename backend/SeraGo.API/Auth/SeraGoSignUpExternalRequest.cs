using System.ComponentModel.DataAnnotations;
using Aufy.Core.Endpoints;

namespace SeraGo.API.Auth;

/// <summary>
/// Body of POST /api/auth/signup/external — the role step a brand-new Google
/// user completes after the OAuth handshake. Email and the provider identity
/// come from Google's claims, never from this payload.
///
/// First/Last name are OPTIONAL here: Google's given_name/family_name claims
/// are used when they're not sent (see <see cref="SeraGoSignUpExternalExtension"/>),
/// so the UI only has to ask for the role.
///
/// Inherits Aufy's <see cref="SignUpExternalRequest"/> because Aufy 1.0.0's
/// <see cref="ISignUpExternalEndpointEvents{TUser,TModel}"/> constrains TModel
/// to it.
/// </summary>
public class SeraGoSignUpExternalRequest : SignUpExternalRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Must be "Talent" or "Recruiter" (validated server-side against the
    /// shared whitelist). No default on purpose — omitting it must fail.
    /// </summary>
    [Required]
    public string? Role { get; set; }

    /// <summary>
    /// Chosen on the role step so the account is created WITH a password from
    /// the start (Google users can then also sign in with email + password).
    /// Validated + hashed in <see cref="SeraGoSignUpExternalExtension"/>
    /// (mirrors Identity's default policy).
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }
}
