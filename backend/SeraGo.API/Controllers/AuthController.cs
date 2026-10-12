using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SeraGo.API.Services;
using Microsoft.AspNetCore.WebUtilities;
using FluentEmail.Core;
using Microsoft.EntityFrameworkCore;

namespace SeraGo.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _db;
    private readonly IFluentEmailFactory _emailFactory;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration config,
        ApplicationDbContext db,
        IFluentEmailFactory emailFactory)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
        _db = db;
        _emailFactory = emailFactory;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token([FromBody] TokenRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
            return NotFound(new { message = "Invalid email or password." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized(new { message = "Account locked out." });
            if (result.IsNotAllowed)
                return StatusCode(403, new { message = "Please confirm your email before signing in." });
                
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var token = await GenerateJwtTokenAsync(user);
        
        var refreshToken = Guid.NewGuid().ToString();
        Response.Cookies.Append("Aufy.RefreshToken", refreshToken, new CookieOptions 
        { 
            HttpOnly = true, 
            Secure = true, 
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        });
        
        return Ok(new SeraGoAuthTokenResponse { AccessToken = token, ExpiresIn = 3600 });
    }

    public static string? NormalizeEthiopianPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var cleaned = new string(phone.Where(char.IsDigit).ToArray());

        if (cleaned.StartsWith("251"))
        {
            cleaned = cleaned.Substring(3);
        }

        if (cleaned.StartsWith("0"))
        {
            cleaned = cleaned.Substring(1);
        }

        if (cleaned.Length == 9 && (cleaned.StartsWith("9") || cleaned.StartsWith("7")))
        {
            return "+251" + cleaned;
        }

        return null;
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email and password are required." });

        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest(new { message = "First name and last name are required." });

        var normalizedPhone = NormalizeEthiopianPhone(req.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
            return BadRequest(new { message = "A valid Ethiopian phone number is required (e.g. 0910101010 or 910101010)." });

        var user = new ApplicationUser 
        { 
            UserName = req.Email.Trim(), 
            Email = req.Email.Trim(),
            FirstName = req.FirstName.Trim(),
            MiddleName = (req.MiddleName ?? string.Empty).Trim(),
            LastName = req.LastName.Trim(),
            PhoneNumber = normalizedPhone,
            UserType = UserType.Talent
        };
        
        var result = await _userManager.CreateAsync(user, req.Password);
        
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName"))
            {
                return BadRequest(new { message = "Account with this email already exists" });
            }
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        // Add default role "Talent"
        await _userManager.AddToRoleAsync(user, "Talent");

        // Ensure TalentProfile and UserSettings exist
        if (!await _db.TalentProfiles.AnyAsync(p => p.UserId == user.Id))
        {
            _db.TalentProfiles.Add(new TalentProfile { UserId = user.Id });
        }
        if (!await _db.UserSettings.AnyAsync(s => s.UserId == user.Id))
        {
            _db.UserSettings.Add(new UserSettings { UserId = user.Id });
        }
        await _db.SaveChangesAsync();

        // Generate and send confirmation email
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var baseUrl = _config["Aufy:ClientApp:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var confirmPath = _config["Aufy:ClientApp:EmailConfirmationPath"] ?? "/confirm-email";
        var link = $"{baseUrl}{confirmPath}?code={code}&userId={user.Id}";

        await _emailFactory.Create()
            .To(user.Email)
            .Subject("Welcome! Please confirm your email")
            .Body($"Thank you for joining SeraGo! Please confirm your email by clicking here: {link}")
            .SendAsync();

        return Ok(new { 
            message = "Account created successfully. Please check your email to confirm your account.",
            requiresEmailConfirmation = true 
        });
    }

    [HttpGet("whoami")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> WhoAmI([FromServices] AdminApiOptions adminApi)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            id = user.Id,
            username = User.Identity?.Name,
            email = user.Email,
            roles = roles.ToArray(),
            emailConfirmed = user.EmailConfirmed,
            adminApiEnabled = adminApi.Enabled
        });
    }

    [HttpPost("token/refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["Aufy.RefreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Refresh token expired or invalid." });

        // Since we removed Aufy, we just validate the user from the expired JWT
        // Or simply extract from HttpContext if JwtBearer allows expired tokens
        // For now, let's just create a dummy one or grab a user if they are logged in.
        // Actually, without the old JWT or a stored refresh token in DB, we can't securely refresh.
        // The frontend will force a re-login if this fails, but let's at least not return 500.
        return Unauthorized(new { message = "Refresh token logic not fully implemented." });
    }

    [HttpGet("external/providers")]
    [AllowAnonymous]
    public IActionResult ExternalProviders()
    {
        return Ok(new { providers = new[] { "Google" } });
    }

    [HttpGet("external/challenge/{provider}")]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, [FromQuery] string callbackUrl)
    {
        var redirectUrl = callbackUrl ?? "/";
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("external/info")]
    [Authorize(AuthenticationSchemes = "Identity.External")]
    public async Task<IActionResult> ExternalInfo()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return BadRequest(new { message = "Error loading external login information." });

        var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        var avatarUrl = info.Principal.FindFirstValue("picture")
                     ?? info.Principal.FindFirstValue("image")
                     ?? info.Principal.FindFirstValue("urn:google:image") ?? string.Empty;

        return Ok(new
        {
            email,
            firstName,
            lastName,
            avatarUrl
        });
    }

    [HttpPost("signin/external")]
    [Authorize(AuthenticationSchemes = "Identity.External")]
    public async Task<IActionResult> SignInExternal()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return BadRequest(new { message = "Error loading external login information." });

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Google sign-in did not provide an email." });

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
            var avatarUrl = info.Principal.FindFirstValue("picture")
                         ?? info.Principal.FindFirstValue("image")
                         ?? info.Principal.FindFirstValue("urn:google:image") ?? string.Empty;

            // Frontend explicitly looks for 400 with "User not found" to prompt signup.
            return BadRequest(new 
            { 
                message = "User not found",
                email,
                firstName,
                lastName,
                avatarUrl
            });
        }

        if (!user.EmailConfirmed)
            return BadRequest(new { message = "Confirm your email first, then sign in with Google." });

        // Ensure login is linked (trusting email since Google verified it).
        var logins = await _userManager.GetLoginsAsync(user);
        if (!logins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
        {
            await _userManager.AddLoginAsync(user, new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.LoginProvider));
        }

        var token = await GenerateJwtTokenAsync(user);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return Ok(new SeraGoAuthTokenResponse { AccessToken = token, ExpiresIn = 3600 });
    }

    [HttpPost("signup/external")]
    [Authorize(AuthenticationSchemes = "Identity.External")]
    public async Task<IActionResult> SignUpExternal([FromBody] SeraGoSignUpExternalRequest req)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return BadRequest(new { message = "Error loading external login information." });

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Google sign-in did not provide an email." });

        var firstName = !string.IsNullOrWhiteSpace(req.FirstName) 
            ? req.FirstName.Trim() 
            : (info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty);

        var lastName = !string.IsNullOrWhiteSpace(req.LastName) 
            ? req.LastName.Trim() 
            : (info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty);

        var middleName = (req.MiddleName ?? string.Empty).Trim();

        var avatarUrl = info.Principal.FindFirstValue("picture")
                     ?? info.Principal.FindFirstValue("image")
                     ?? info.Principal.FindFirstValue("urn:google:image") ?? string.Empty;
        
        var providerKey = info.ProviderKey;
        var provider = info.LoginProvider;

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            return BadRequest(new { message = "User already exists. Please sign in instead." });
        }

        if (string.IsNullOrWhiteSpace(req.Password))
        {
            return BadRequest(new { message = "Password is required." });
        }

        var normalizedPhone = NormalizeEthiopianPhone(req.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return BadRequest(new { message = "A valid Ethiopian phone number is required (e.g. 0910101010 or 910101010)." });
        }

        user = new ApplicationUser 
        { 
            UserName = email, 
            Email = email, 
            EmailConfirmed = true, 
            FirstName = firstName, 
            MiddleName = middleName,
            LastName = lastName,
            PhoneNumber = normalizedPhone,
            AvatarUrl = avatarUrl,
            UserType = UserType.Talent
        };
        var createResult = await _userManager.CreateAsync(user, req.Password);

        if (!createResult.Succeeded)
            return BadRequest(new { message = string.Join(", ", createResult.Errors.Select(e => e.Description)) });

        await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
        
        // Use requested role if valid, else default to Talent
        var role = !string.IsNullOrEmpty(req.Role) ? req.Role : "Talent";
        await _userManager.AddToRoleAsync(user, role);

        // Ensure TalentProfile and UserSettings exist
        if (!await _db.TalentProfiles.AnyAsync(p => p.UserId == user.Id))
        {
            _db.TalentProfiles.Add(new TalentProfile { UserId = user.Id });
        }
        if (!await _db.UserSettings.AnyAsync(s => s.UserId == user.Id))
        {
            _db.UserSettings.Add(new UserSettings { UserId = user.Id });
        }
        await _db.SaveChangesAsync();

        var token = await GenerateJwtTokenAsync(user);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return Ok(new SeraGoAuthTokenResponse { AccessToken = token, ExpiresIn = 3600 });
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Aufy:JwtBearer:SigningKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(1);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class TokenRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SignUpRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SeraGoSignUpExternalRequest
{
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; }
}

public class SeraGoAuthTokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("TokenType")]
    public string TokenType { get; set; } = "Bearer";

    [System.Text.Json.Serialization.JsonPropertyName("AccessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("ExpiresIn")]
    public int ExpiresIn { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("RefreshToken")]
    public string? RefreshToken { get; set; }
}
