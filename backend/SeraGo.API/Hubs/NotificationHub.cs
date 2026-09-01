using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace SeraGo.API.Hubs;

/// <summary>
/// SignalR hub for real-time notification delivery.
///
/// Users connect with a JWT token. The hub tracks which users are online
/// via Redis sets (user:{userId}:active → set of connection IDs).
/// Notifications are pushed to a user's group so all their devices receive updates.
///
/// If Redis is unavailable, the hub still works — it just can't track
/// online presence for smart routing.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(
        IConnectionMultiplexer? redis,
        ILogger<NotificationHub> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            // Add this connection to the user's group (for targeted pushes)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            // Track connection in Redis for online presence
            await RegisterConnectionAsync(userId);

            _logger.LogDebug(
                "SignalR connected: user={UserId} connection={ConnectionId}",
                userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            // Remove from Redis presence tracking
            await UnregisterConnectionAsync(userId);

            _logger.LogDebug(
                "SignalR disconnected: user={UserId} connection={ConnectionId}",
                userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Clients can call this to request their current unread count.
    /// Useful after reconnection when the client might have missed pushes.
    /// </summary>
    public async Task GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId is null) return;

        // The client should use the REST API for this,
        // but this is a convenience method for reconnection sync.
        await Clients.Caller.SendAsync("unread_count_request_ack");
    }

    // ── Helpers ──

    private string? GetUserId()
    {
        // Aufy/JWT puts the user ID in the NameIdentifier claim
        return Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");
    }

    private string ActiveKey(string userId) => $"user:{userId}:active";

    private async Task RegisterConnectionAsync(string userId)
    {
        if (_redis is null) return;

        try
        {
            var db = _redis.GetDatabase();
            await db.SetAddAsync(ActiveKey(userId), Context.ConnectionId);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex,
                "Failed to register SignalR connection in Redis for user {UserId}", userId);
        }
    }

    private async Task UnregisterConnectionAsync(string userId)
    {
        if (_redis is null) return;

        try
        {
            var db = _redis.GetDatabase();
            await db.SetRemoveAsync(ActiveKey(userId), Context.ConnectionId);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex,
                "Failed to unregister SignalR connection in Redis for user {UserId}", userId);
        }
    }
}
