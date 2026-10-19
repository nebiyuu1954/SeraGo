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
/// GET  /api/telegram/link       — get a t.me deep link that starts linking
/// POST /api/telegram/link       — validate a linking token (legacy flow)
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

        // GET returns a ready-to-open t.me deep link (token embedded) — the flow
        // the frontend uses, instead of asking the user to copy a token.
        group.MapGet("/link", GetLinkAsync)
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

        // Manual trigger for channel sweep
        group.MapPost("/sweep", () =>
        {
            Hangfire.BackgroundJob.Enqueue<TelegramJobService>(s => s.SweepPendingChannelJobsAsync());
            return Results.Ok(new { message = "Channel sweep enqueued successfully" });
        })
        .WithOpenApi();

        // Webhook registration trigger
        group.MapPost("/webhook/register", async (TelegramBotService svc) =>
        {
            var webhookUrl = Environment.GetEnvironmentVariable("TELEGRAM_WEBHOOK_URL")
                ?? "https://api.serago.pro.et/api/telegram/webhook";
            var success = await svc.RegisterWebhookAsync(webhookUrl);
            return success
                ? Results.Ok(new { message = $"Webhook registered to {webhookUrl}" })
                : Results.Problem("Failed to register webhook");
        })
        // Connect chat endpoint — called by the special /telegram/connect page
        group.MapPost("/connect-chat", ConnectChatAsync)
            .RequireAuthorization()
            .WithOpenApi();

        return app;
    }

    // ── GET /api/telegram/link ──

    private static async Task<IResult> GetLinkAsync(
        TelegramBotService telegramService,
        HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (userId is null)
            return Results.Unauthorized();

        var username = await telegramService.GetBotUsernameAsync();
        if (string.IsNullOrWhiteSpace(username))
            return Results.Problem(
                "Telegram bot is not configured.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        var token = telegramService.GenerateLinkingToken(userId);
        var url = $"https://t.me/{username}?start={token}";

        return Results.Ok(new { url, token });
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
        HttpRequest request,
        TelegramBotService telegramService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("TelegramWebhook");
        try
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                return Results.Ok();
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var telegramUpdate = System.Text.Json.JsonSerializer.Deserialize<Telegram.Bot.Types.Update>(json, options);

            if (telegramUpdate is not null)
            {
                await telegramService.HandleMessageAsync(telegramUpdate);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process incoming Telegram webhook update");
        }

        return Results.Ok();
    }

    private static async Task<IResult> ConnectChatAsync(
        [FromBody] TelegramConnectChatRequest request,
        TelegramBotService telegramService,
        HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (userId is null)
            return Results.Unauthorized();

        var validatedChatId = telegramService.ValidateChatLinkingToken(request.Token);
        if (validatedChatId is null || validatedChatId.Value != request.ChatId)
        {
            return Results.BadRequest(new { message = "Invalid or expired linking token." });
        }

        var linked = await telegramService.LinkAccountAsync(userId, request.ChatId);
        if (!linked)
        {
            return Results.Problem("Could not link account.");
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await telegramService.SendTalentDashboardCardAsync(
                    request.ChatId,
                    userId,
                    "🎉 <b>Your SeraGo account has been successfully connected!</b>");
            }
            catch { }
        });

        return Results.Ok(new { success = true });
    }

    // ── DTOs ──

    public sealed record TelegramLinkRequest(string Token);
    public sealed record TelegramConnectChatRequest(long ChatId, string Token);
}
