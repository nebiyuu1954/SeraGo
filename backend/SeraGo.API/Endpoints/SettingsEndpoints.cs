using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// GET/PUT/PATCH /api/account/settings — the current user's own settings.
///
/// Settings are stored as a versioned JSON blob. The frontend controls the
/// shape; the backend treats it as opaque JSON. External services read
/// settings via internal API endpoints.
///
/// PUT semantics: full replace — send the entire settings object.
/// PATCH semantics: partial update — merge the sent category into the existing blob.
/// </summary>
public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account").WithTags("Settings");

        group.MapGet("/settings", GetSettingsAsync).WithOpenApi();
        group.MapPut("/settings", UpdateSettingsAsync).WithOpenApi();
        group.MapPatch("/settings/{category}", PatchSettingsCategoryAsync).WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record GetSettingsResponse(string Settings, int Version);

    public sealed class UpdateSettingsRequest
    {
        /// <summary>The full settings JSON string.</summary>
        public string Settings { get; set; } = "{}";

        /// <summary>Optimistic concurrency version — must match the current version.</summary>
        public int Version { get; set; }
    }

    public sealed class PatchSettingsCategoryRequest
    {
        /// <summary>The category key (e.g. "account", "notifications", "ai").</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>The category's new value as a JSON string.</summary>
        public string Value { get; set; } = "{}";
    }

    // -------------------------------------------------------------- Handlers

    [Authorize]
    private static async Task<IResult> GetSettingsAsync(
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null) return Results.Unauthorized();

        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (settings is null)
        {
            // First access — return defaults (empty settings, version 1).
            return Results.Ok(new GetSettingsResponse("{}", 1));
        }

        return Results.Ok(new GetSettingsResponse(settings.Settings, settings.Version));
    }

    [Authorize]
    private static async Task<IResult> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null) return Results.Unauthorized();

        // Validate the settings JSON is well-formed.
        if (!IsValidJson(request.Settings))
        {
            return Results.Problem(
                "Settings must be valid JSON.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (settings is null)
        {
            settings = new UserSettings { UserId = user.Id };
            db.UserSettings.Add(settings);
        }
        else if (settings.Version != request.Version)
        {
            return Results.Problem(
                "Settings have been modified since you loaded them. Please refresh and try again.",
                statusCode: StatusCodes.Status409Conflict);
        }

        settings.Settings = request.Settings;
        settings.Version += 1;
        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(new GetSettingsResponse(settings.Settings, settings.Version));
    }

    [Authorize]
    private static async Task<IResult> PatchSettingsCategoryAsync(
        string category,
        PatchSettingsCategoryRequest request,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null) return Results.Unauthorized();

        // Validate category key — only lowercase alphanumeric + hyphens.
        if (string.IsNullOrWhiteSpace(category) || !System.Text.RegularExpressions.Regex.IsMatch(category, @"^[a-z][a-z0-9\-]*$"))
        {
            return Results.Problem(
                "Category must be a lowercase alphanumeric key (e.g. 'account', 'notifications').",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Validate the category value is well-formed JSON.
        if (!IsValidJson(request.Value))
        {
            return Results.Problem(
                "Category value must be valid JSON.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (settings is null)
        {
            settings = new UserSettings { UserId = user.Id };
            db.UserSettings.Add(settings);
        }

        // Merge the category into the existing settings blob.
        var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(settings.Settings) ?? [];
        var categoryValue = JsonSerializer.Deserialize<JsonElement>(request.Value);
        root[category] = categoryValue;

        settings.Settings = JsonSerializer.Serialize(root);
        settings.Version += 1;
        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(new GetSettingsResponse(settings.Settings, settings.Version));
    }

    // --------------------------------------------------------------- Helpers

    private static bool IsValidJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
