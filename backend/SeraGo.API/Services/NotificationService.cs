using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Hubs;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;
using StackExchange.Redis;

namespace SeraGo.API.Services;

/// <summary>
/// Core notification logic — creates notifications, manages unread counts,
/// and coordinates delivery across channels (in-app, email, Telegram).
///
/// PostgreSQL is the source of truth. Redis caches unread counts for O(1)
/// reads. SignalR pushes real-time updates to online users. Hangfire handles
/// async email/Telegram delivery.
/// </summary>
public sealed class NotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationService> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IHubContext<NotificationHub>? _hubContext;

    public NotificationService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationService> logger,
        IConnectionMultiplexer? redis = null,
        IHubContext<NotificationHub>? hubContext = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _redis = redis;
        _hubContext = hubContext;
    }

    // ══════════════════════════════════════════════════════════════════
    //  DTOs
    // ══════════════════════════════════════════════════════════════════

    public sealed record CreateNotificationRequest(
        string UserId,
        string Type,
        string Title,
        string Body,
        string? Data = null,
        bool SkipEmail = false,
        bool SkipTelegram = false,
        Guid? BatchId = null);

    public sealed record BatchNotificationItem(
        string UserId,
        string Type,
        string Title,
        string Body,
        string? Data = null);

    public sealed record BatchNotificationRequest(
        List<BatchNotificationItem> Notifications,
        bool DeferEmail = true,
        bool DeferTelegram = true,
        Guid? BatchId = null);

    public sealed record NotificationDto(
        Guid Id,
        string Type,
        string Title,
        string Body,
        string? Data,
        bool IsRead,
        DateTimeOffset CreatedAt);

    public sealed record PagedNotifications(
        List<NotificationDto> Notifications,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);

    public sealed record MarkReadResult(int MarkedCount);

    public sealed record UnreadCountResult(int Count);

    public sealed record BatchResult(Guid BatchId, int CreatedCount);

    // ══════════════════════════════════════════════════════════════════
    //  CREATE — Single notification
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a single notification and triggers delivery across all enabled channels.
    /// </summary>
    public async Task<Guid?> CreateAsync(CreateNotificationRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Read per-type channel preferences
        var prefs = await GetChannelPreferencesAsync(db, request.UserId, request.Type);

        // If in-app is disabled, skip creating the notification entirely
        if (!prefs.InApp)
        {
            Console.WriteLine($"[NotificationService] SKIPPED notification for user {request.UserId} type={request.Type} — in-app disabled");
            _logger.LogInformation(
                "Skipping notification for user {UserId} type={Type} — in-app disabled in settings",
                request.UserId, request.Type);
            return null;
        }

        var notification = new Notification
        {
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title,
            Body = request.Body,
            Data = request.Data,
            EmailBatchId = request.BatchId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Created notification {NotificationId} for user {UserId} type={Type}",
            notification.Id, request.UserId, request.Type);

        // Update Redis unread count
        await IncrementUnreadCountAsync(request.UserId);

        // Push via SignalR if user is online
        await PushIfOnlineAsync(request.UserId, notification);

        // Enqueue Hangfire jobs — respect per-type channel prefs + caller overrides
        var skipEmail = request.SkipEmail || !prefs.Email;
        var skipTelegram = request.SkipTelegram || !prefs.Telegram;
        await EnqueueDeliveryJobsAsync(notification, skipEmail, skipTelegram);

        return notification.Id;
    }

    // ══════════════════════════════════════════════════════════════════
    //  CREATE — Batch notifications
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a batch of notifications in a single INSERT. Used by Django AI
    /// for bulk job matches (e.g., 5,000 notifications at 3 AM).
    /// </summary>
    public async Task<BatchResult> CreateBatchAsync(BatchNotificationRequest request)
    {
        var batchId = request.BatchId ?? Guid.NewGuid();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Filter by per-user channel preferences
        var notifications = new List<Notification>();
        foreach (var item in request.Notifications)
        {
            var prefs = await GetChannelPreferencesAsync(db, item.UserId, item.Type);
            if (!prefs.InApp)
            {
                Console.WriteLine($"[NotificationService] Batch SKIPPED for user {item.UserId} type={item.Type} — in-app disabled");
                continue;
            }
            notifications.Add(new Notification
            {
                UserId = item.UserId,
                Type = item.Type,
                Title = item.Title,
                Body = item.Body,
                Data = item.Data,
                EmailBatchId = batchId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (notifications.Count == 0)
            return new BatchResult(batchId, 0);

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Created batch {BatchId} with {Count} notifications",
            batchId, notifications.Count);

        // Update Redis counters per user
        var userCounts = notifications
            .GroupBy(n => n.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (userId, count) in userCounts)
        {
            await IncrementUnreadCountAsync(userId, count);

            // Push batch update if user is online
            await PushBatchIfOnlineAsync(userId, count);
        }

        // Enqueue delivery jobs — respect per-type channel prefs + caller defer flags
        if (!request.DeferEmail || !request.DeferTelegram)
        {
            foreach (var notification in notifications)
            {
                var prefs = await GetChannelPreferencesAsync(db, notification.UserId, notification.Type);
                var skipEmail = request.DeferEmail || !prefs.Email;
                var skipTelegram = request.DeferTelegram || !prefs.Telegram;
                await EnqueueDeliveryJobsAsync(notification, skipEmail, skipTelegram);
            }
        }

        return new BatchResult(batchId, notifications.Count);
    }

    // ══════════════════════════════════════════════════════════════════
    //  READ — Get paginated notifications
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns paginated notifications for a user, newest first.
    /// </summary>
    public async Task<PagedNotifications> GetNotificationsAsync(
        string userId, int page = 1, int pageSize = 20, string? type = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var query = db.Notifications
            .Where(n => n.UserId == userId);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(n => n.Type == type);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.Data,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync();

        return new PagedNotifications(
            items,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling((double)totalCount / pageSize));
    }

    // ══════════════════════════════════════════════════════════════════
    //  READ — Unread count
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the unread notification count. Reads from Redis first (O(1)),
    /// falls back to Postgres COUNT(*) if the Redis key is missing (cold start).
    /// </summary>
    public async Task<UnreadCountResult> GetUnreadCountAsync(string userId)
    {
        var count = await GetUnreadCountFromRedisOrDbAsync(userId);
        return new UnreadCountResult(count);
    }

    // ══════════════════════════════════════════════════════════════════
    //  MARK READ — Single or all
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Marks specific notifications as read.
    /// </summary>
    public async Task<MarkReadResult> MarkReadAsync(string userId, List<Guid> notificationIds)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTimeOffset.UtcNow;

        var updated = await db.Notifications
            .Where(n => n.UserId == userId
                && notificationIds.Contains(n.Id)
                && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        if (updated > 0)
        {
            await DecrementUnreadCountAsync(userId, updated);
            await PushUnreadCountUpdateAsync(userId);
        }

        _logger.LogInformation(
            "User {UserId} marked {Count} notifications as read",
            userId, updated);

        return new MarkReadResult(updated);
    }

    /// <summary>
    /// Marks ALL unread notifications as read.
    /// </summary>
    public async Task<MarkReadResult> MarkAllReadAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTimeOffset.UtcNow;

        var updated = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        if (updated > 0)
        {
            await SetUnreadCountAsync(userId, 0);
            await PushUnreadCountUpdateAsync(userId);
        }

        _logger.LogInformation(
            "User {UserId} marked all ({Count}) notifications as read",
            userId, updated);

        return new MarkReadResult(updated);
    }

    // ══════════════════════════════════════════════════════════════════
    //  REDIS — Unread count management
    // ══════════════════════════════════════════════════════════════════

    private string UnreadCountKey(string userId) => $"user:{userId}:unread_count";

    private async Task IncrementUnreadCountAsync(string userId, int amount = 1)
    {
        if (_redis is null) return;

        try
        {
            var db = _redis.GetDatabase();
            await db.StringIncrementAsync(UnreadCountKey(userId), amount);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis INCR failed for user {UserId}, falling back to Postgres", userId);
        }
    }

    private async Task DecrementUnreadCountAsync(string userId, int amount)
    {
        if (_redis is null) return;

        try
        {
            var db = _redis.GetDatabase();
            var newVal = await db.StringDecrementAsync(UnreadCountKey(userId), amount);

            // Safety clamp: if counter went negative due to race condition, set to 0
            if (newVal < 0)
            {
                await db.StringSetAsync(UnreadCountKey(userId), 0);
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis DECR failed for user {UserId}", userId);
        }
    }

    private async Task SetUnreadCountAsync(string userId, int count)
    {
        if (_redis is null) return;

        try
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(UnreadCountKey(userId), count);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for user {UserId}", userId);
        }
    }

    private async Task<int> GetUnreadCountFromRedisOrDbAsync(string userId)
    {
        // Try Redis first
        if (_redis is not null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var val = await db.StringGetAsync(UnreadCountKey(userId));
                if (val.HasValue)
                    return (int)val;
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis GET failed for user {UserId}, falling back to Postgres", userId);
            }
        }

        // Fall back to Postgres COUNT(*)
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var count = await dbContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        // Cache in Redis for next time
        await SetUnreadCountAsync(userId, count);

        return count;
    }

    // ══════════════════════════════════════════════════════════════════
    //  SIGNALR — Real-time push
    // ══════════════════════════════════════════════════════════════════

    private string ActiveKey(string userId) => $"user:{userId}:active";

    private async Task<bool> IsUserOnlineAsync(string userId)
    {
        if (_redis is null) return false;

        try
        {
            var db = _redis.GetDatabase();
            var count = await db.SetLengthAsync(ActiveKey(userId));
            return count > 0;
        }
        catch (RedisException)
        {
            return false;
        }
    }

    private async Task PushIfOnlineAsync(string userId, Notification notification)
    {
        if (_hubContext is null) return;
        if (!await IsUserOnlineAsync(userId)) return;

        var count = await GetUnreadCountFromRedisOrDbAsync(userId);

        var dto = new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.Data,
            notification.IsRead,
            notification.CreatedAt);

        await _hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("new_notification", new { unreadCount = count, notification = dto });
    }

    private async Task PushBatchIfOnlineAsync(string userId, int newCount)
    {
        if (_hubContext is null) return;
        if (!await IsUserOnlineAsync(userId)) return;

        var totalCount = await GetUnreadCountFromRedisOrDbAsync(userId);

        await _hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("unread_count_updated", new { count = totalCount });
    }

    private async Task PushUnreadCountUpdateAsync(string userId)
    {
        if (_hubContext is null) return;
        if (!await IsUserOnlineAsync(userId)) return;

        var count = await GetUnreadCountFromRedisOrDbAsync(userId);

        await _hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("unread_count_updated", new { count });
    }

    // ══════════════════════════════════════════════════════════════════
    //  HANGFIRE — Enqueue delivery jobs
    // ══════════════════════════════════════════════════════════════════

    private async Task EnqueueDeliveryJobsAsync(
        Notification notification, bool skipEmail, bool skipTelegram)
    {
        try
        {
            if (!skipEmail)
            {
                BackgroundJob.Enqueue<NotificationOrchestrator>(o =>
                    o.SendEmailNotification(notification.Id));
                _logger.LogDebug(
                    "Enqueued email job for notification {Id}", notification.Id);
            }

            if (!skipTelegram)
            {
                BackgroundJob.Enqueue<NotificationOrchestrator>(o =>
                    o.SendTelegramNotification(notification.Id));
                _logger.LogDebug(
                    "Enqueued Telegram job for notification {Id}", notification.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to enqueue delivery for notification {Id}", notification.Id);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SETTINGS — Check user's notification preferences per channel
    // ══════════════════════════════════════════════════════════════════

    private sealed record ChannelPreferences(bool Email, bool InApp, bool Telegram);

    private static string NotificationTypeToSettingsKey(string notificationType) => notificationType switch
    {
        "application_received" => "newApplications",
        "application_status_changed" => "applicationUpdates",
        "job_alert" or "saved_search_match" => "jobAlerts",
        "admin_review_result" => "jobStatusChanges",
        "system_announcement" => "systemAlerts",
        _ => ""
    };

    /// <summary>
    /// Reads per-type channel preferences (email / inApp / telegram) from the UserSettings JSON blob.
    /// New format: notifications.{typeKey} = { email: bool, inApp: bool, telegram: bool }
    /// Falls back to legacy format (notifications.inApp.{typeKey} / notifications.email.{typeKey}) for
    /// users who haven't updated their settings yet. Defaults to all-true if nothing found.
    /// </summary>
    private static async Task<ChannelPreferences> GetChannelPreferencesAsync(
        ApplicationDbContext db, string userId, string notificationType)
    {
        var settingsJson = await db.UserSettings
            .Where(s => s.UserId == userId)
            .Select(s => s.Settings)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(settingsJson))
            return new ChannelPreferences(true, true, true);

        try
        {
            var settings = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(settingsJson);
            var notifSection = settings.GetProperty("notifications");
            var key = NotificationTypeToSettingsKey(notificationType);

            if (string.IsNullOrEmpty(key))
                return new ChannelPreferences(true, true, true);

            // New format: notifications.{key} = { email, inApp, telegram }
            if (notifSection.TryGetProperty(key, out var typeObj) && typeObj.ValueKind == JsonValueKind.Object)
            {
                var email = typeObj.TryGetProperty("email", out var e) && e.GetBoolean();
                var inApp = typeObj.TryGetProperty("inApp", out var i) && i.GetBoolean();
                var telegram = typeObj.TryGetProperty("telegram", out var t) && t.GetBoolean();

                var prefs = new ChannelPreferences(email, inApp, telegram);
                Console.WriteLine($"[NotificationService] User {userId} type={notificationType} prefs={prefs} settings={settingsJson}");
                return prefs;
            }

            // Legacy format fallback: notifications.inApp.{key} and notifications.email.{key}
            var legacyInApp = true;
            var legacyEmail = true;
            if (notifSection.TryGetProperty("inApp", out var inAppSection) && inAppSection.TryGetProperty(key, out var inAppVal))
                legacyInApp = inAppVal.GetBoolean();
            if (notifSection.TryGetProperty("email", out var emailSection) && emailSection.TryGetProperty(key, out var emailVal))
                legacyEmail = emailVal.GetBoolean();

            var legacyPrefs = new ChannelPreferences(legacyEmail, legacyInApp, false);
            Console.WriteLine($"[NotificationService] User {userId} type={notificationType} LEGACY prefs={legacyPrefs} settings={settingsJson}");
            return legacyPrefs;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Failed to read settings for user {userId}: {ex.Message} settings={settingsJson}");
            return new ChannelPreferences(true, true, true);
        }
    }
}
