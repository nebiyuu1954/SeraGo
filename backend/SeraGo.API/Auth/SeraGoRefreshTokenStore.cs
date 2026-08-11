using Aufy.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Auth;

/// <summary>
/// Corrected replacement for Aufy 1.0.0's
/// <c>RefreshTokenStore&lt;TContext, TUser&gt;</c>.
///
/// The shipped store has a bug in <c>SaveAsync</c>: it calls
/// <c>FindAsync(new object[] { refreshToken.UserId, ct })</c>, passing the
/// cancellation token as a SECOND key value. The AufyRefreshTokens table's
/// primary key is UserId alone, so that lookup never matches and the store
/// ALWAYS inserts — every sign-in after the first (without an intervening
/// sign-out) crashes with a UNIQUE constraint violation on UserId.
///
/// This store looks the row up by UserId correctly and UPDATES the existing
/// token instead of inserting a duplicate, so a user keeps a single refresh
/// token that is rotated on each sign-in.
///
/// Every operation runs on its OWN DbContext scope (created from the root
/// scope factory) — never the request's scoped context. Reason: Aufy's
/// sign-out handler calls <c>ClearTokenAsync</c> WITHOUT awaiting it, so the
/// DELETE outlives the request. On the request's shared connection that races
/// with the context being disposed at request end, which surfaces as Npgsql
/// "This method may not be called when another read operation is pending"
/// 500s on /api/auth/signout (a remote DB like Neon makes the race reliably
/// visible; local SQLite usually won in time). A per-operation scope keeps
/// every token read/write on a connection only that operation ever touches.
///
/// <see cref="DeleteByUserIdAsync"/> swallows failures with a warning (not
/// an error): besides the fire-and-forget sign-out path, the password-reset
/// endpoint awaits it too, and a failed cleanup must not fail a successful
/// reset. Tradeoff: a refresh token that fails to delete stays valid until
/// expiry — acceptable for best-effort cleanup.
/// </summary>
public class SeraGoRefreshTokenStore(
    IServiceScopeFactory scopeFactory,
    ILogger<SeraGoRefreshTokenStore> logger) : IRefreshTokenStore
{
    public async Task<AufyRefreshToken?> FindByUserIdAsync(string userId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.RefreshTokens.FindAsync(new object[] { userId }, ct);
    }

    public async Task SaveAsync(AufyRefreshToken refreshToken, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Not atomic: two simultaneous sign-ins for the same user could both
        // see no existing row and both INSERT. The token endpoint effectively
        // serializes per user in practice, so accept the assumption.
        var existing = await context.RefreshTokens
            .FindAsync(new object[] { refreshToken.UserId }, ct);
        if (existing is null)
        {
            await context.RefreshTokens.AddAsync(refreshToken, ct);
        }
        else
        {
            // Rotate in place — one refresh token per user.
            existing.RefreshToken = refreshToken.RefreshToken;
            existing.ExpiresAt = refreshToken.ExpiresAt;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteByUserIdAsync(string userId)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.RefreshTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync();
        }
        catch (Exception ex)
        {
            // Best-effort cleanup: this runs fire-and-forget from Aufy's
            // sign-out handler, so an unobserved failure would otherwise log
            // as an unhandled exception. Token expiry covers the rare case
            // where the deletion is genuinely lost.
            logger.LogWarning(ex, "Failed to delete refresh token for user {UserId}", userId);
        }
    }
}
