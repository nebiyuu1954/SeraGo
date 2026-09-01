using System.Text;
using System.Text.Json;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using SeraGo.Infrastructure.Context;
using StackExchange.Redis;
using Telegram.Bot;

namespace SeraGo.API.Services;

/// <summary>
/// Hangfire job definitions for notification delivery.
/// Jobs are enqueued by NotificationService and processed asynchronously.
///
/// Queue isolation ensures one slow channel doesn't block others:
///   - "email"       → email delivery + digest assembly
///   - "telegram"    → Telegram bot delivery
///   - "maintenance" → cleanup + Redis sync
/// </summary>
public sealed class NotificationOrchestrator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationOrchestrator> _logger;
    private readonly IFluentEmail _fluentEmail;

    public NotificationOrchestrator(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationOrchestrator> logger,
        IFluentEmail fluentEmail)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _fluentEmail = fluentEmail;
    }

    // ══════════════════════════════════════════════════════════════════
    //  EMAIL DELIVERY
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sends an email notification for a single notification record.
    /// Enqueued by NotificationService after creating the in-app notification.
    /// </summary>
    [Queue("email")]
    [AutomaticRetry(Attempts = 3)]
    public async Task SendEmailNotification(Guid notificationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<ISender>();

        var notification = await db.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification is null)
        {
            _logger.LogWarning("Notification {Id} not found, skipping email", notificationId);
            return;
        }

        if (notification.EmailSent)
        {
            _logger.LogDebug("Notification {Id} already sent via email, skipping", notificationId);
            return;
        }

        // Check user's email preferences
        var settings = await GetUserEmailSettingsAsync(db, notification.UserId, notification.Type);
        if (!settings)
        {
            _logger.LogInformation(
                "Email disabled for user {UserId} type={Type}, skipping",
                notification.UserId, notification.Type);
            return;
        }

        // Build email
        var userEmail = notification.User?.Email;
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            _logger.LogWarning("User {UserId} has no email, skipping", notification.UserId);
            return;
        }

        var subject = notification.Title;
        var htmlBody = BuildEmailHtml(notification);

        try
        {
            // Use FluentEmail fluent API to compose and send
            var email = _fluentEmail
                .To(userEmail)
                .Subject(subject)
                .Body(htmlBody, isHtml: true);

            await emailSender.SendAsync(email);

            // Mark as sent
            notification.EmailSent = true;
            notification.EmailSentAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation(
                "Email sent for notification {Id} to {Email}", notificationId, userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email for notification {Id}", notificationId);
            throw; // Hangfire will retry
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  TELEGRAM DELIVERY
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sends a Telegram message for a single notification record.
    /// </summary>
    [Queue("telegram")]
    [AutomaticRetry(Attempts = 3)]
    public async Task SendTelegramNotification(Guid notificationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var telegramClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        var notification = await db.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification is null)
        {
            _logger.LogWarning("Notification {Id} not found, skipping Telegram", notificationId);
            return;
        }

        if (notification.TelegramSent)
        {
            _logger.LogDebug("Notification {Id} already sent via Telegram, skipping", notificationId);
            return;
        }

        // Check if user has Telegram linked
        var chatId = await GetTelegramChatIdAsync(db, notification.UserId);
        if (chatId is null)
        {
            _logger.LogDebug("User {UserId} has no Telegram linked, skipping", notification.UserId);
            return;
        }

        // Check user's Telegram preferences
        var settings = await GetUserTelegramSettingsAsync(db, notification.UserId, notification.Type);
        if (!settings)
        {
            _logger.LogInformation(
                "Telegram disabled for user {UserId} type={Type}, skipping",
                notification.UserId, notification.Type);
            return;
        }

        try
        {
            var message = BuildTelegramMessage(notification);
            await telegramClient.SendMessage(chatId.Value, message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

            notification.TelegramSent = true;
            notification.TelegramSentAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation(
                "Telegram sent for notification {Id} to chat {ChatId}", notificationId, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send Telegram for notification {Id}", notificationId);
            throw; // Hangfire will retry
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  DIGEST EMAIL (Daily at 8 AM)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Assembles and sends digest emails for batch notifications.
    /// Groups unsent notifications by batch_id and user, composes one email per user.
    /// </summary>
    [Queue("email")]
    [AutomaticRetry(Attempts = 2)]
    public async Task SendDigestEmails()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<ISender>();

        _logger.LogInformation("Starting digest email job");

        // Find all batch_ids from last 24 hours that haven't been digested
        var pendingBatchIds = await db.Notifications
            .Where(n => n.EmailBatchId != null
                && !n.EmailSent
                && n.CreatedAt > DateTimeOffset.UtcNow.AddHours(-24))
            .Select(n => n.EmailBatchId!.Value)
            .Distinct()
            .ToListAsync();

        _logger.LogInformation("Found {Count} pending digest batches", pendingBatchIds.Count);

        var totalSent = 0;

        foreach (var batchId in pendingBatchIds)
        {
            // Group by user
            var userGroups = await db.Notifications
                .Where(n => n.EmailBatchId == batchId && !n.EmailSent)
                .GroupBy(n => n.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count(),
                    Notifications = g.ToList()
                })
                .ToListAsync();

            foreach (var userGroup in userGroups)
            {
                // Check email preferences
                var settings = await GetUserEmailSettingsAsync(db, userGroup.UserId, "job_alert");
                if (!settings) continue;

                // Get user email
                var user = await db.Users.FindAsync(userGroup.UserId);
                if (user?.Email is null) continue;

                // Compose digest email
                var subject = $"📬 {userGroup.Count} new updates from SeraGo";
                var htmlBody = BuildDigestEmailHtml(userGroup.Notifications);

                try
                {
                    var email = _fluentEmail
                        .To(user.Email)
                        .Subject(subject)
                        .Body(htmlBody, isHtml: true);

                    await emailSender.SendAsync(email);

                    // Mark all as sent
                    foreach (var notif in userGroup.Notifications)
                    {
                        notif.EmailSent = true;
                        notif.EmailSentAt = DateTimeOffset.UtcNow;
                    }
                    await db.SaveChangesAsync();

                    totalSent += userGroup.Count;
                    _logger.LogInformation(
                        "Digest sent to {Email}: {Count} notifications",
                        user.Email, userGroup.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send digest to {Email}", user.Email);
                }
            }
        }

        _logger.LogInformation("Digest email job complete. Sent {Total} notifications", totalSent);
    }

    // ══════════════════════════════════════════════════════════════════
    //  CLEANUP (Weekly on Sunday at 3 AM)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deletes notifications older than 90 days, batched to avoid long locks.
    /// </summary>
    [Queue("maintenance")]
    public async Task CleanupOldNotifications()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.LogInformation("Starting notification cleanup job");

        var cutoff = DateTimeOffset.UtcNow.AddDays(-90);
        var totalDeleted = 0;

        // Delete in batches of 10,000 to avoid long-running transactions
        while (true)
        {
            var batch = await db.Notifications
                .Where(n => n.CreatedAt < cutoff)
                .Take(10_000)
                .Select(n => n.Id)
                .ToListAsync();

            if (batch.Count == 0) break;

            await db.Notifications
                .Where(n => batch.Contains(n.Id))
                .ExecuteDeleteAsync();

            totalDeleted += batch.Count;
            _logger.LogInformation("Cleanup: deleted {Count} notifications (total: {Total})",
                batch.Count, totalDeleted);

            // Small delay between batches to reduce lock contention
            await Task.Delay(100);
        }

        _logger.LogInformation("Cleanup complete. Deleted {Total} notifications older than 90 days",
            totalDeleted);
    }

    // ══════════════════════════════════════════════════════════════════
    //  REDIS COUNTER SYNC (Hourly)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Safety net: syncs Redis unread counters with Postgres truth.
    /// Catches drift from failed INCR operations or Redis restarts.
    /// </summary>
    [Queue("maintenance")]
    public async Task SyncRedisCounters()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var redis = scope.ServiceProvider.GetService<IConnectionMultiplexer>();

        if (redis is null)
        {
            _logger.LogDebug("Redis not configured, skipping counter sync");
            return;
        }

        _logger.LogInformation("Starting Redis counter sync job");

        // Get active users (logged in within 7 days)
        var activeUserIds = await db.Users
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .Take(10_000) // limit to prevent runaway job
            .ToListAsync();

        var dbInst = redis.GetDatabase();
        var driftCount = 0;

        foreach (var userId in activeUserIds)
        {
            var key = $"user:{userId}:unread_count";

            // Get Postgres truth
            var dbCount = await db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            // Get Redis value
            var redisVal = await dbInst.StringGetAsync(key);
            var redisCount = redisVal.HasValue ? (int)redisVal : -1;

            if (redisCount != dbCount)
            {
                await dbInst.StringSetAsync(key, dbCount);
                driftCount++;
                _logger.LogWarning(
                    "Counter drift: user={UserId} redis={Redis} postgres={Postgres}",
                    userId, redisCount, dbCount);
            }
        }

        _logger.LogInformation(
            "Redis counter sync complete. Checked {Count} users, fixed {Drift} drifts",
            activeUserIds.Count, driftCount);
    }

    // ══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════

    private static string TypeToSettingsKey(string notificationType) => notificationType switch
    {
        "application_received" => "newApplications",
        "application_status_changed" => "applicationUpdates",
        "job_alert" or "saved_search_match" => "jobAlerts",
        "admin_review_result" or "system_announcement" => "systemAlerts",
        _ => ""
    };

    private async Task<bool> GetUserEmailSettingsAsync(
        ApplicationDbContext db, string userId, string notificationType)
    {
        var settingsJson = await db.UserSettings
            .Where(s => s.UserId == userId)
            .Select(s => s.Settings)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            _logger.LogInformation("[EmailCheck] No settings for user {UserId}, defaulting to enabled", userId);
            return true;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<JsonElement>(settingsJson);
            var notifSection = settings.GetProperty("notifications");
            var key = TypeToSettingsKey(notificationType);

            if (string.IsNullOrEmpty(key))
            {
                _logger.LogInformation("[EmailCheck] Unknown type {Type} for user {UserId}, defaulting to enabled", notificationType, userId);
                return true;
            }

            // New format: notifications.{key}.email
            if (notifSection.TryGetProperty(key, out var typeObj) && typeObj.ValueKind == JsonValueKind.Object)
            {
                if (typeObj.TryGetProperty("email", out var emailVal))
                {
                    var enabled = emailVal.GetBoolean();
                    _logger.LogInformation("[EmailCheck] User {UserId} type={Type} key={Key} email={Enabled} (new format)", userId, notificationType, key, enabled);
                    return enabled;
                }
            }

            // Legacy fallback: notifications.email.{key}
            if (notifSection.TryGetProperty("email", out var emailSection) && emailSection.TryGetProperty(key, out var legacyVal))
            {
                var enabled = legacyVal.GetBoolean();
                _logger.LogInformation("[EmailCheck] User {UserId} type={Type} key={Key} email={Enabled} (legacy format)", userId, notificationType, key, enabled);
                return enabled;
            }

            _logger.LogInformation("[EmailCheck] User {UserId} type={Type} key={Key} not found in settings, defaulting to enabled", userId, notificationType, key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[EmailCheck] Failed to read settings for user {UserId}, defaulting to enabled. Settings: {Settings}", userId, settingsJson);
            return true;
        }
    }

    private async Task<bool> GetUserTelegramSettingsAsync(
        ApplicationDbContext db, string userId, string notificationType)
    {
        var settingsJson = await db.UserSettings
            .Where(s => s.UserId == userId)
            .Select(s => s.Settings)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(settingsJson)) return false; // default: disabled (not linked)

        try
        {
            var settings = JsonSerializer.Deserialize<JsonElement>(settingsJson);

            // Check if Telegram is linked
            if (settings.TryGetProperty("telegram", out var telegram))
            {
                if (!telegram.GetProperty("linked").GetBoolean()) return false;
            }
            else
            {
                return false;
            }

            // Check notification preferences
            var notifSettings = settings.GetProperty("notifications");
            if (notifSettings.TryGetProperty("telegram", out var telegramPrefs))
            {
                return notificationType switch
                {
                    "application_received" => telegramPrefs.GetProperty("newApplications").GetBoolean(),
                    "application_status_changed" => telegramPrefs.GetProperty("applicationUpdates").GetBoolean(),
                    "job_alert" or "saved_search_match" => telegramPrefs.GetProperty("jobAlerts").GetBoolean(),
                    _ => true
                };
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<long?> GetTelegramChatIdAsync(ApplicationDbContext db, string userId)
    {
        var settingsJson = await db.UserSettings
            .Where(s => s.UserId == userId)
            .Select(s => s.Settings)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(settingsJson)) return null;

        try
        {
            var settings = JsonSerializer.Deserialize<JsonElement>(settingsJson);
            if (settings.TryGetProperty("telegram", out var telegram)
                && telegram.GetProperty("linked").GetBoolean())
            {
                return telegram.GetProperty("chatId").GetInt64();
            }
        }
        catch { }

        return null;
    }

    private string BuildEmailHtml(Core.Domain.Entities.Notification notification)
    {
        var data = notification.Data ?? "{}";
        var dataObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data) ?? new();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'></head><body style='font-family: -apple-system, BlinkMacSystemFont, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>");
        sb.AppendLine($"<h2 style='color: #1a1a2e;'>{EscapeHtml(notification.Title)}</h2>");
        sb.AppendLine($"<p style='color: #555; font-size: 16px;'>{EscapeHtml(notification.Body)}</p>");

        // Add action buttons based on type
        if (notification.Type == "application_received" && dataObj.TryGetValue("job_id", out var jobId))
        {
            sb.AppendLine($"<p><a href='https://serago.et/dashboard/applications' style='display: inline-block; background: #6750A4; color: white; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600;'>View Application</a></p>");
        }
        else if (notification.Type == "job_alert")
        {
            sb.AppendLine($"<p><a href='https://serago.et/dashboard/jobs' style='display: inline-block; background: #6750A4; color: white; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600;'>View Jobs</a></p>");
        }

        sb.AppendLine("<hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>");
        sb.AppendLine("<p style='color: #999; font-size: 12px;'>SeraGo Job Board · <a href='https://serago.et/dashboard/settings'>Manage notification preferences</a></p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private string BuildDigestEmailHtml(List<Core.Domain.Entities.Notification> notifications)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'></head><body style='font-family: -apple-system, BlinkMacSystemFont, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>");
        sb.AppendLine("<h2 style='color: #1a1a2e;'>📬 Your Daily Digest</h2>");
        sb.AppendLine($"<p style='color: #555;'>You have {notifications.Count} new updates:</p>");

        foreach (var notif in notifications.Take(20)) // max 20 in email
        {
            sb.AppendLine("<div style='border: 1px solid #eee; border-radius: 8px; padding: 12px; margin: 8px 0;'>");
            sb.AppendLine($"<strong style='color: #1a1a2e;'>{EscapeHtml(notif.Title)}</strong>");
            sb.AppendLine($"<p style='color: #555; margin: 4px 0 0 0;'>{EscapeHtml(notif.Body)}</p>");
            sb.AppendLine("</div>");
        }

        if (notifications.Count > 20)
        {
            sb.AppendLine($"<p style='color: #999;'>... and {notifications.Count - 20} more</p>");
        }

        sb.AppendLine($"<p style='margin-top: 20px;'><a href='https://serago.et/dashboard/notifications' style='display: inline-block; background: #6750A4; color: white; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600;'>View All Notifications</a></p>");
        sb.AppendLine("<hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>");
        sb.AppendLine("<p style='color: #999; font-size: 12px;'>SeraGo Job Board · <a href='https://serago.et/dashboard/settings'>Manage notification preferences</a></p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private string BuildTelegramMessage(Core.Domain.Entities.Notification notification)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>{EscapeHtml(notification.Title)}</b>");
        sb.AppendLine();
        sb.AppendLine(EscapeHtml(notification.Body));

        // Add action links
        if (notification.Type == "application_received")
        {
            sb.AppendLine();
            sb.AppendLine("<a href='https://serago.et/dashboard/applications'>View Application →</a>");
        }
        else if (notification.Type == "job_alert")
        {
            sb.AppendLine();
            sb.AppendLine("<a href='https://serago.et/dashboard/jobs'>View Jobs →</a>");
        }

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("/unsubscribe to stop notifications");

        return sb.ToString();
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
