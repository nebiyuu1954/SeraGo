using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;

using SeraGo.API.Services;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Account lifecycle endpoints for the current user:
///
///   POST   /api/account/deactivate — soft disable (IsActive = false).
///          Also blocks password sign-in via Identity's lockout and revokes
///          refresh tokens, so the account can't re-authenticate after the
///          access token expires. Reversible in principle, but there is no
///          reactivation endpoint yet — restoring an account currently needs
///          an admin action or direct DB change (clear LockoutEnd, set
///          IsActive = true).
///   DELETE /api/account             — permanent deletion. Requires the
///          account password as confirmation. Profile rows and the Identity
///          tables (roles, claims, logins, tokens) cascade off AspNetUsers;
///          refresh tokens are deleted explicitly because that table has no
///          foreign key.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account").WithTags("Account");

        group.MapPost("/deactivate", DeactivateAccountAsync).WithOpenApi();
        group.MapDelete("/", DeleteAccountAsync).WithOpenApi();
        group.MapPost("/password/set", SetPasswordAsync).WithOpenApi();

        group.MapGet("/connected-accounts", GetConnectedAccountsAsync).WithOpenApi();
        group.MapPost("/connected-accounts/telegram/link", LinkTelegramAccountAsync).WithOpenApi();
        group.MapPost("/connected-accounts/telegram/unlink", UnlinkTelegramAccountAsync).WithOpenApi();

        return app;
    }

    public sealed record DeleteAccountRequest(string Password);
    public sealed record SetPasswordRequest(string Password);

    [Authorize]
    private static async Task<IResult> DeactivateAccountAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        // Self-healing: also run when the account is already inactive but was
        // never fully disabled (e.g. IsActive flipped by another path without
        // the lockout), so deactivate always ends in a locked-out state.
        if (user.IsActive || user.LockoutEnd is null)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            // Block password sign-in. Identity's sign-in flow checks
            // IsLockedOut() before the password regardless of lockoutOnFailure,
            // so an active lockout always refuses the sign-in — this is the
            // standard "disabled account" pattern.
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            await RevokeRefreshTokensAsync(db, user.Id);
        }

        await userManager.UpdateAsync(user);
        return Results.Ok(new { message = "Account deactivated. Sign out and discard any stored tokens." });
    }

    [Authorize]
    private static async Task<IResult> DeleteAccountAsync(
        [FromBody] DeleteAccountRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        // Irreversible action: confirm the caller knows the account password.
        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Results.Problem(
                "Incorrect password. Account was not deleted.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await RevokeRefreshTokensAsync(db, user.Id);

        // TalentProfile/RecruiterProfile and the Identity tables
        // (roles, claims, logins, tokens) cascade off AspNetUsers.
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Results.Problem(
                "Could not delete the account: " + string.Join(", ", result.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Sets a password on an account that doesn't have one yet (e.g. a Google
    /// sign-up). Once set, the user can sign in with email + password on any
    /// device and use the regular forgot-password flow.
    ///
    /// POST /api/account/password/set — authenticated.
    /// </summary>
    [Authorize]
    private static async Task<IResult> SetPasswordAsync(
        [FromBody] SetPasswordRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Problem(
                "Password is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Defense-in-depth: this endpoint is guarded only by the bearer token,
        // so require a confirmed email before letting a session plant a
        // password. Google-created accounts are always EmailConfirmed = true
        // (the OAuth handshake confirms it), so the legitimate flow is never
        // blocked — this only stops unconfirmed/abandoned sessions.
        if (!user.EmailConfirmed)
        {
            return Results.Problem(
                "Confirm your email address before setting a password.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Fails with "User already has a password." when one exists — for
        // changing an existing password use the Aufy change-password endpoint.
        var result = await userManager.AddPasswordAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Results.Problem(
                string.Join(", ", result.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok();
    }

    [Authorize]
    private static async Task<IResult> GetConnectedAccountsAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var logins = await userManager.GetLoginsAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);

        var googleLogin = logins.FirstOrDefault(l => string.Equals(l.LoginProvider, "Google", StringComparison.OrdinalIgnoreCase));
        var telegramLogin = logins.FirstOrDefault(l => string.Equals(l.LoginProvider, "Telegram", StringComparison.OrdinalIgnoreCase));

        var settingsJson = await db.UserSettings
            .Where(s => s.UserId == user.Id)
            .Select(s => s.Settings)
            .FirstOrDefaultAsync();

        var telegramLinked = telegramLogin != null;
        string? telegramUsername = null;
        long? telegramChatId = telegramLogin != null && long.TryParse(telegramLogin.ProviderKey, out var tid) ? tid : null;

        if (!string.IsNullOrWhiteSpace(settingsJson))
        {
            try
            {
                var settings = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(settingsJson);
                if (settings.TryGetProperty("telegram", out var tg))
                {
                    if (tg.TryGetProperty("linked", out var linkedProp) && linkedProp.GetBoolean())
                    {
                        telegramLinked = true;
                    }
                    if (tg.TryGetProperty("username", out var u) && u.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        telegramUsername = u.GetString();
                    }
                    if (tg.TryGetProperty("chatId", out var c) && c.TryGetInt64(out var cid))
                    {
                        telegramChatId = cid;
                    }
                }
            }
            catch { }
        }

        var accounts = new List<object>
        {
            new
            {
                provider = "Google",
                displayName = "Google",
                connected = googleLogin != null,
                email = googleLogin != null ? user.Email : null
            },
            new
            {
                provider = "Telegram",
                displayName = "Telegram",
                connected = telegramLinked,
                username = telegramUsername,
                id = telegramChatId?.ToString()
            }
        };

        return Results.Ok(new
        {
            hasPassword,
            accounts
        });
    }

    public sealed record LinkTelegramAccountRequest(
        long Id,
        string? FirstName,
        string? LastName,
        string? Username,
        string? PhotoUrl,
        long AuthDate,
        string Hash);

    [Authorize]
    private static async Task<IResult> LinkTelegramAccountAsync(
        [FromBody] LinkTelegramAccountRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ITelegramAuthService telegramAuth)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var payload = new TelegramAuthPayload(
            request.Id, request.FirstName, request.LastName, request.Username, request.PhotoUrl, request.AuthDate, request.Hash);

        if (!telegramAuth.Verify(payload))
        {
            return Results.Problem("Invalid or expired Telegram authentication signature.", statusCode: StatusCodes.Status400BadRequest);
        }

        // Prevent linking if already linked to another SeraGo user
        var existingUser = await userManager.FindByLoginAsync("Telegram", request.Id.ToString());
        if (existingUser != null && existingUser.Id != user.Id)
        {
            return Results.Problem("This Telegram account is already linked to another SeraGo profile.", statusCode: StatusCodes.Status400BadRequest);
        }

        var logins = await userManager.GetLoginsAsync(user);
        if (!logins.Any(l => string.Equals(l.LoginProvider, "Telegram", StringComparison.OrdinalIgnoreCase)))
        {
            var addLoginResult = await userManager.AddLoginAsync(user, new UserLoginInfo("Telegram", request.Id.ToString(), "Telegram"));
            if (!addLoginResult.Succeeded)
            {
                return Results.Problem("Failed to link Telegram login: " + string.Join(", ", addLoginResult.Errors.Select(e => e.Description)), statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // Update UserSettings so Telegram bot notifications and DMs work
        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (settings == null)
        {
            settings = new UserSettings { UserId = user.Id };
            db.UserSettings.Add(settings);
        }

        var settingsObj = string.IsNullOrWhiteSpace(settings.Settings)
            ? new Dictionary<string, object>()
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(settings.Settings) ?? new();

        settingsObj["telegram"] = new Dictionary<string, object>
        {
            ["linked"] = true,
            ["chatId"] = request.Id,
            ["username"] = request.Username ?? string.Empty,
            ["linkedAt"] = DateTimeOffset.UtcNow.ToString("O")
        };

        settings.Settings = System.Text.Json.JsonSerializer.Serialize(settingsObj);
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            message = "Telegram account linked successfully.",
            username = request.Username,
            id = request.Id.ToString()
        });
    }

    [Authorize]
    private static async Task<IResult> UnlinkTelegramAccountAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IServiceProvider sp)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var logins = await userManager.GetLoginsAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);

        // Security guard: If this is the user's only login method and they don't have a password, block unlinking
        if (!hasPassword && logins.Count <= 1 && logins.Any(l => string.Equals(l.LoginProvider, "Telegram", StringComparison.OrdinalIgnoreCase)))
        {
            return Results.Problem(
                "Cannot disconnect your only sign-in method. Please set a password first in the Security tab.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var telegramLogin = logins.FirstOrDefault(l => string.Equals(l.LoginProvider, "Telegram", StringComparison.OrdinalIgnoreCase));
        if (telegramLogin != null)
        {
            await userManager.RemoveLoginAsync(user, telegramLogin.LoginProvider, telegramLogin.ProviderKey);
        }

        // Unlink in UserSettings
        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (settings != null && !string.IsNullOrWhiteSpace(settings.Settings))
        {
            try
            {
                var settingsObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(settings.Settings) ?? new();
                if (settingsObj.TryGetValue("telegram", out var tgVal) && tgVal is System.Text.Json.JsonElement tgEl)
                {
                    var tgDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tgEl.GetRawText()) ?? new();
                    tgDict["linked"] = false;
                    settingsObj["telegram"] = tgDict;
                    settings.Settings = System.Text.Json.JsonSerializer.Serialize(settingsObj);
                    settings.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
            catch { }
        }

        var botService = sp.GetService<TelegramBotService>();
        if (botService != null)
        {
            await botService.UnlinkAccountAsync(user.Id);
        }

        return Results.Ok(new { message = "Telegram account disconnected." });
    }

    /// <summary>AufyRefreshTokens has no FK to AspNetUsers — remove rows explicitly.</summary>
    private static Task<int> RevokeRefreshTokensAsync(ApplicationDbContext db, string userId) =>
        db.RefreshTokens.Where(rt => rt.UserId == userId).ExecuteDeleteAsync();
}
