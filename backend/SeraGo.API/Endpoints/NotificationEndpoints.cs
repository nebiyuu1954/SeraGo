using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeraGo.API.Services;

namespace SeraGo.API.Endpoints;

/// <summary>
/// In-app notification endpoints.
///
/// GET    /api/notifications              — paginated notification list
/// GET    /api/notifications/unread-count — badge count (Redis-backed)
/// PATCH  /api/notifications/read         — mark specific notifications as read
/// PATCH  /api/notifications/read-all     — mark all as read
/// POST   /api/notifications/batch        — create batch (internal, API key auth)
/// GET    /api/notifications/settings     — get notification preferences
/// PUT    /api/notifications/settings     — update notification preferences
/// </summary>
public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        // Authenticated user endpoints
        group.MapGet("/", GetNotificationsAsync)
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/unread-count", GetUnreadCountAsync)
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPatch("/read", MarkReadAsync)
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPatch("/read-all", MarkAllReadAsync)
            .RequireAuthorization()
            .WithOpenApi();

        // Internal batch endpoint (API key auth, not JWT)
        group.MapPost("/batch", CreateBatchAsync)
            .RequireAuthorization("InternalApi")
            .WithOpenApi();

        // Admin test endpoint — simulates scraper job alerts for testing
        group.MapPost("/simulate-job-alerts", SimulateJobAlertsAsync)
            .RequireAuthorization("InternalApi")
            .WithOpenApi();

        return app;
    }

    // ── Helpers ──

    private static string GetCurrentUserId(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("No user ID in token");
    }

    // ── GET /api/notifications ──

    private static async Task<IResult> GetNotificationsAsync(
        [AsParameters] NotificationQueryParams queryParams,
        NotificationService notificationService,
        HttpContext context)
    {
        var userId = GetCurrentUserId(context);

        var result = await notificationService.GetNotificationsAsync(
            userId,
            queryParams.Page,
            queryParams.PageSize,
            queryParams.Type);

        return Results.Ok(result);
    }

    // ── GET /api/notifications/unread-count ──

    private static async Task<IResult> GetUnreadCountAsync(
        NotificationService notificationService,
        HttpContext context)
    {
        var userId = GetCurrentUserId(context);

        var result = await notificationService.GetUnreadCountAsync(userId);

        return Results.Ok(result);
    }

    // ── PATCH /api/notifications/read ──

    private static async Task<IResult> MarkReadAsync(
        [FromBody] MarkReadRequest request,
        NotificationService notificationService,
        HttpContext context)
    {
        var userId = GetCurrentUserId(context);

        var result = await notificationService.MarkReadAsync(userId, request.NotificationIds);

        return Results.Ok(result);
    }

    // ── PATCH /api/notifications/read-all ──

    private static async Task<IResult> MarkAllReadAsync(
        NotificationService notificationService,
        HttpContext context)
    {
        var userId = GetCurrentUserId(context);

        var result = await notificationService.MarkAllReadAsync(userId);

        return Results.Ok(result);
    }

    // ── POST /api/notifications/batch ──

    private static async Task<IResult> CreateBatchAsync(
        [FromBody] NotificationService.BatchNotificationRequest request,
        NotificationService notificationService)
    {
        var result = await notificationService.CreateBatchAsync(request);

        return Results.Accepted($"/api/notifications/batch/{result.BatchId}", result);
    }

    // ── POST /api/notifications/simulate-job-alerts ──
    // Admin-only endpoint to simulate scraper batch job notifications.
    // Sends a "New job posted" alert to the calling user.

    private static async Task<IResult> SimulateJobAlertsAsync(
        [FromBody] SimulateJobAlertRequest request,
        NotificationService notificationService,
        HttpContext context)
    {
        var userId = GetCurrentUserId(context);

        // Create a job alert notification for the current user
        var notificationId = await notificationService.CreateAsync(new NotificationService.CreateNotificationRequest(
            UserId: userId,
            Type: Core.Domain.Enums.NotificationType.JobAlert,
            Title: request.Title ?? "New job matches found",
            Body: request.Body ?? $"{request.JobCount} new jobs match your profile in {request.SectorName ?? "Technology"}",
            Data: $"{{\"total_count\":{request.JobCount},\"sector_name\":\"{request.SectorName ?? "General"}\"}}",
            SkipEmail: request.DeferEmail,
            SkipTelegram: true
        ));

        return Results.Ok(new { notificationId, message = "Job alert notification created" });
    }

    // ══════════════════════════════════════════════════════════════════
    //  Request/Response DTOs
    // ══════════════════════════════════════════════════════════════════

    public sealed record SimulateJobAlertRequest(
        int JobCount = 5,
        string? SectorName = "Technology",
        string? Title = null,
        string? Body = null,
        bool DeferEmail = true
    );

    public sealed class NotificationQueryParams
    {
        [FromQuery(Name = "page")]
        public int Page { get; set; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; } = 20;

        [FromQuery(Name = "type")]
        public string? Type { get; set; }
    }

    public sealed record MarkReadRequest(List<Guid> NotificationIds);
}
