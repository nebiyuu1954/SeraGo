using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Services;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Telegram account linking endpoints.
///
/// POST /api/telegram/link       — link Telegram account with a token
/// POST /api/telegram/unlink     — unlink Telegram account
/// GET  /api/telegram/status     — check if Telegram is linked
/// POST /api/telegram/webhook    — Telegram bot webhook (no auth)
/// </summary>
public static class TelegramEndpoints
{
    public static IEndpointRouteBuilder MapTelegramEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/telegram").WithTags("Telegram");

        group.MapPost("/link", LinkAccountAsync)
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/unlink", UnlinkAccountAsync)
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/status", GetStatusAsync)
            .RequireAuthorization()
            .WithOpenApi();

        // Webhook endpoint — called by Telegram servers, no JWT auth
        group.MapPost("/webhook", HandleWebhookAsync)
            .WithOpenApi();

        return app;
    }

    // ── POST /api/telegram/link ──

    private static async Task<IResult> LinkAccountAsync(
        [FromBody] TelegramLinkRequest request,
        TelegramBotService telegramService,
        HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (userId is null)
            return Results.Unauthorized();

        // Validate the linking token
        var linkedUserId = telegramService.ValidateLinkingToken(request.Token);
        if (linkedUserId is null)
            return Results.BadRequest(new { message = "Invalid or expired linking token" });

        // The token must belong to the current user
        if (linkedUserId != userId)
            return Results.Forbid();

        // We need the chat_id from the bot — the token alone isn't enough.
        // The flow should be:
        // 1. User clicks link in Telegram → bot receives /start {token}
        // 2. Bot calls POST /api/telegram/webhook with the token + chat_id
        // 3. Or: frontend generates token, user pastes it in Telegram, bot stores pending link
        //
        // For now, we'll store the token as pending and complete the link
        // when the bot confirms via webhook.
        return Results.Ok(new
        {
            message = "Linking token accepted. Please send /start with this token in Telegram to complete linking.",
            token = request.Token
        });
    }

    // ── POST /api/telegram/unlink ──

    private static async Task<IResult> UnlinkAccountAsync(
        TelegramBotService telegramService,
        HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (userId is null)
            return Results.Unauthorized();

        var result = await telegramService.UnlinkAccountAsync(userId);

        return result
            ? Results.Ok(new { message = "Telegram account unlinked" })
            : Results.NotFound(new { message = "No settings found" });
    }

    // ── GET /api/telegram/status ──

    private static async Task<IResult> GetStatusAsync(
        [FromServices] ApplicationDbContext db,
        HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (userId is null)
            return Results.Unauthorized();

        var settingsJson = await db.UserSettings
            .Where(s => s.UserId == userId)
            .Select(s => s.Settings)
            .FirstOrDefaultAsync();

        var isLinked = false;
        if (!string.IsNullOrWhiteSpace(settingsJson))
        {
            try
            {
                var settings = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(settingsJson);
                if (settings.TryGetProperty("telegram", out var telegram))
                {
                    isLinked = telegram.GetProperty("linked").GetBoolean();
                }
            }
            catch { }
        }

        return Results.Ok(new { linked = isLinked });
    }

    // ── POST /api/telegram/webhook ──

    private static async Task<IResult> HandleWebhookAsync(
        [FromBody] object update,
        TelegramBotService telegramService)
    {
        // Parse the Telegram update
        var json = System.Text.Json.JsonSerializer.Serialize(update);
        var telegramUpdate = System.Text.Json.JsonSerializer.Deserialize<Telegram.Bot.Types.Update>(json);

        if (telegramUpdate is not null)
        {
            await telegramService.HandleMessageAsync(telegramUpdate);
        }

        return Results.Ok();
    }

    // ── DTOs ──

    public sealed record TelegramLinkRequest(string Token);
}
