using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Services;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

public record CreateScheduledMessageRequest(
    string Title,
    TelegramMessageTarget Target = TelegramMessageTarget.Both,
    TelegramMessageType MessageType = TelegramMessageType.GoodMorning,
    string DaysOfWeek = "[\"*\"]",
    string? SpecificDate = null,
    string ScheduledTime = "08:00",
    string? CustomTemplate = null,
    string? BannerUrl = null,
    string? ButtonText = null,
    string? ButtonUrl = null,
    bool IsActive = true);

public record UpdateScheduledMessageRequest(
    string Title,
    TelegramMessageTarget Target,
    TelegramMessageType MessageType,
    string DaysOfWeek,
    string? SpecificDate,
    string ScheduledTime,
    string? CustomTemplate,
    string? BannerUrl,
    string? ButtonText,
    string? ButtonUrl,
    bool IsActive);

public static class AdminTelegramMessageEndpoints
{
    public static IEndpointRouteBuilder MapAdminTelegramMessageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/telegram/messages")
            .WithTags("Admin Telegram Messaging")
            .RequireAuthorization("Admin");

        // ── List & Get ───────────────────────────────────────────────────────
        group.MapGet("/", async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var items = await db.TelegramScheduledMessages
                .AsNoTracking()
                .OrderBy(m => m.ScheduledTime)
                .ThenBy(m => m.Title)
                .ToListAsync(ct);

            return Results.Ok(items);
        })
        .WithOpenApi();

        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var item = await db.TelegramScheduledMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        })
        .WithOpenApi();

        // ── Create ───────────────────────────────────────────────────────────
        group.MapPost("/", async (
            [FromBody] CreateScheduledMessageRequest req,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title))
            {
                return Results.BadRequest(new { message = "Title is required." });
            }

            var message = new TelegramScheduledMessage
            {
                Id = Guid.NewGuid(),
                Title = req.Title.Trim(),
                Target = req.Target,
                MessageType = req.MessageType,
                DaysOfWeek = !string.IsNullOrWhiteSpace(req.DaysOfWeek) ? req.DaysOfWeek : "[\"*\"]",
                SpecificDate = !string.IsNullOrWhiteSpace(req.SpecificDate) ? req.SpecificDate.Trim() : null,
                ScheduledTime = !string.IsNullOrWhiteSpace(req.ScheduledTime) ? req.ScheduledTime.Trim() : "08:00",
                CustomTemplate = !string.IsNullOrWhiteSpace(req.CustomTemplate) ? req.CustomTemplate : null,
                BannerUrl = !string.IsNullOrWhiteSpace(req.BannerUrl) ? req.BannerUrl.Trim() : null,
                ButtonText = !string.IsNullOrWhiteSpace(req.ButtonText) ? req.ButtonText.Trim() : null,
                ButtonUrl = !string.IsNullOrWhiteSpace(req.ButtonUrl) ? req.ButtonUrl.Trim() : null,
                IsActive = req.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.TelegramScheduledMessages.Add(message);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/admin/telegram/messages/{message.Id}", message);
        })
        .WithOpenApi();

        // ── Update ───────────────────────────────────────────────────────────
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateScheduledMessageRequest req,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var message = await db.TelegramScheduledMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (message is null) return Results.NotFound();

            message.Title = req.Title.Trim();
            message.Target = req.Target;
            message.MessageType = req.MessageType;
            message.DaysOfWeek = !string.IsNullOrWhiteSpace(req.DaysOfWeek) ? req.DaysOfWeek : "[\"*\"]";
            message.SpecificDate = !string.IsNullOrWhiteSpace(req.SpecificDate) ? req.SpecificDate.Trim() : null;
            message.ScheduledTime = !string.IsNullOrWhiteSpace(req.ScheduledTime) ? req.ScheduledTime.Trim() : "08:00";
            message.CustomTemplate = !string.IsNullOrWhiteSpace(req.CustomTemplate) ? req.CustomTemplate : null;
            message.BannerUrl = !string.IsNullOrWhiteSpace(req.BannerUrl) ? req.BannerUrl.Trim() : null;
            message.ButtonText = !string.IsNullOrWhiteSpace(req.ButtonText) ? req.ButtonText.Trim() : null;
            message.ButtonUrl = !string.IsNullOrWhiteSpace(req.ButtonUrl) ? req.ButtonUrl.Trim() : null;
            message.IsActive = req.IsActive;
            message.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(message);
        })
        .WithOpenApi();

        // ── Toggle Active Status ─────────────────────────────────────────────
        group.MapPatch("/{id:guid}/toggle", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var message = await db.TelegramScheduledMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (message is null) return Results.NotFound();

            message.IsActive = !message.IsActive;
            message.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { message.Id, message.IsActive });
        })
        .WithOpenApi();

        // ── Delete ───────────────────────────────────────────────────────────
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var message = await db.TelegramScheduledMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (message is null) return Results.NotFound();

            db.TelegramScheduledMessages.Remove(message);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithOpenApi();

        // ── Test Send ────────────────────────────────────────────────────────
        group.MapPost("/{id:guid}/test", async (
            Guid id,
            [FromBody] TestSendRequest req,
            ClaimsPrincipal user,
            TelegramBroadcastDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await dispatcher.ExecuteTestSendAsync(id, req, userId, ct);

            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithOpenApi();

        // ── Get Testing Config Status ────────────────────────────────────────
        group.MapGet("/test-config", async (
            ClaimsPrincipal user,
            ApplicationDbContext db,
            TelegramOptions options,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            long? adminChatId = null;
            var adminIsLinked = false;

            if (!string.IsNullOrEmpty(userId))
            {
                var settings = await db.UserSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
                if (settings is not null)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(settings.Settings);
                        if (doc.RootElement.TryGetProperty("telegram", out var tg))
                        {
                            if (tg.TryGetProperty("linked", out var l) && l.GetBoolean()) adminIsLinked = true;
                            if (tg.TryGetProperty("chatId", out var cid)) adminChatId = cid.GetInt64();
                        }
                    }
                    catch { }
                }
            }

            return Results.Ok(new
            {
                testChannelId = options.TestChannelId,
                hasTestChannel = options.HasTestChannel,
                testChatId = options.TestChatId,
                hasTestChat = options.HasTestChat,
                messagingTestMode = options.MessagingTestMode,
                adminIsLinked,
                adminChatId,
                productionChannelId = options.ChannelId
            });
        })
        .WithOpenApi();

        // ── Seed Default Templates ───────────────────────────────────────────
        group.MapPost("/seed-defaults", async ([FromQuery] bool force, ApplicationDbContext db, CancellationToken ct) =>
        {
            await TelegramBroadcastDispatcher.SeedDefaultSchedulesAsync(db, force);
            var all = await db.TelegramScheduledMessages.AsNoTracking().ToListAsync(ct);
            return Results.Ok(all);
        })
        .WithOpenApi();

        return app;
    }
}
