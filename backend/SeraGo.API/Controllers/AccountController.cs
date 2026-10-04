using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SeraGo.API.Email;
using SeraGo.Core.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using FluentEmail.Core;

namespace SeraGo.API.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly EmailThrottleService _throttle;
    private readonly IFluentEmailFactory _emailFactory;
    private readonly IConfiguration _config;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger,
        EmailThrottleService throttle,
        IFluentEmailFactory emailFactory,
        IConfiguration config)
    {
        _userManager = userManager;
        _logger = logger;
        _throttle = throttle;
        _emailFactory = emailFactory;
        _config = config;
    }

    [HttpGet("email/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            return BadRequest("Invalid confirmation request.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        if (user.EmailConfirmed)
            return Ok(new { message = "Email is already confirmed." });

        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

        if (result.Succeeded)
            return Ok(new { message = "Email confirmed successfully." });

        return BadRequest("Invalid or expired confirmation code.");
    }

    [HttpPost("email/confirm/resend")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest();

        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
            return NotFound(new { message = "No account found with this email address." });

        if (user.EmailConfirmed)
            return Conflict(new { message = "This email is already verified — you can sign in now." });

        if (!_throttle.TryAllow(req.Email, out var retryAfterMessage))
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = retryAfterMessage });

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var baseUrl = _config["Aufy:ClientApp:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var confirmPath = _config["Aufy:ClientApp:EmailConfirmationPath"] ?? "/confirm-email";
        var link = $"{baseUrl}{confirmPath}?code={code}&userId={user.Id}";

        await _emailFactory.Create()
            .To(user.Email)
            .Subject("Confirm your email address")
            .Body($"Please confirm your email by clicking here: {link}")
            .SendAsync();

        _logger.LogInformation("Confirmation email sent to {Email}", user.Email);
        return Ok();
    }

    [HttpPost("password/forgot")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] PasswordForgotRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest();

        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
            return NotFound(new { message = "No account found with this email address." });

        if (!_throttle.TryAllow(req.Email, out var retryAfterMessage))
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = retryAfterMessage });

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var baseUrl = _config["Aufy:ClientApp:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var resetPath = _config["Aufy:ClientApp:PasswordResetPath"] ?? "/reset-password";
        var link = $"{baseUrl}{resetPath}?code={code}&email={Uri.EscapeDataString(user.Email!)}";

        await _emailFactory.Create()
            .To(user.Email)
            .Subject("Reset your password")
            .Body($"Please reset your password by clicking here: {link}")
            .SendAsync();

        _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        return Ok();
    }
}

public class ResendConfirmationRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class PasswordForgotRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
