using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SeraGo.Core.Domain.Entities;
using SeraGo.API.Services;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Replacement for Aufy 1.0.0's <c>WhoAmIEndpoint</c> (GET /api/auth/whoami).
/// The shipped endpoint builds its response purely from JWT claims, so it has
/// no way to report whether the account email is confirmed — which the
/// frontend needs to block unconfirmed users from the dashboards.
///
/// This endpoint mirrors the same response shape (camelCase
/// { username, email, roles }) and adds <c>emailConfirmed</c>, loaded from the
/// user row, plus <c>adminApiEnabled</c> so the frontend can hide admin UI
/// when the kill switch is off.
/// </summary>
public static class SeraGoWhoAmIEndpoint
{
    public static IEndpointRouteBuilder MapSeraGoWhoAmIEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/whoami", HandleAsync)
            .RequireAuthorization()
            .WithOpenApi();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        AdminApiOptions adminApi)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new
        {
            username = claims.Identity?.Name,
            email = user.Email,
            roles = (await userManager.GetRolesAsync(user)).ToArray(),
            emailConfirmed = user.EmailConfirmed,
            adminApiEnabled = adminApi.Enabled,
        });
    }
}
