using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain;
using SeraGo.Core.Domain.Entities;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Admin user management:
///   GET    /api/admin/users            — list all users with pagination + search
///   GET    /api/admin/users/{id}       — user detail
///   PATCH  /api/admin/users/{id}/role  — change user role
///   PATCH  /api/admin/users/{id}/status — activate/deactivate
/// </summary>
public static class AdminUserEndpoints
{
    public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users").WithTags("Admin Users");

        group.MapGet("/", ListUsersAsync).WithOpenApi();
        group.MapGet("/{id}", GetUserAsync).WithOpenApi();
        group.MapPatch("/{id}/role", UpdateRoleAsync).WithOpenApi();
        group.MapPatch("/{id}/status", UpdateStatusAsync).WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed record UserResponse(
        string Id,
        string FirstName,
        string MiddleName,
        string LastName,
        string Email,
        string UserType,
        bool IsActive,
        bool EmailConfirmed,
        string? AvatarUrl,
        string? City,
        string? Country,
        string CreatedAt);

    public sealed record UserListData(
        List<UserResponse> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage);

    public sealed record UpdateRoleRequest(string Role);
    public sealed record UpdateStatusRequest(bool IsActive);

    public sealed class UserListQuery
    {
        public string? Q { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    private const int MaxPageSize = 50;

    // ------------------------------------------------------------- Handlers

    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> ListUsersAsync(
        [AsParameters] UserListQuery query,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var q = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = $"%{EscapeLike(query.Q)}%";
            q = q.Where(u =>
                EF.Functions.ILike(u.FirstName, pattern, "\\") ||
                EF.Functions.ILike(u.LastName, pattern, "\\") ||
                EF.Functions.ILike(u.Email!, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var roleLower = query.Role.Trim().ToLowerInvariant();
            q = q.Where(u => u.UserType.ToString().ToLowerInvariant() == roleLower);
        }

        if (query.IsActive is not null)
        {
            q = q.Where(u => u.IsActive == query.IsActive.Value);
        }

        var totalCount = await q.CountAsync();

        q = q.OrderByDescending(u => u.CreatedAt);

        var page = Math.Clamp(query.Page ?? 1, 1, 100_000);
        var pageSize = Math.Clamp(query.PageSize ?? 20, 1, MaxPageSize);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return Results.Ok(new UserListData(
            items.Select(ToResponse).ToList(),
            totalCount, page, pageSize, totalPages, page < totalPages));
    }

    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> GetUserAsync(
        string id,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.NotFound();
        return Results.Ok(ToResponse(user));
    }

    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> UpdateRoleAsync(
        string id,
        UpdateRoleRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.NotFound();

        var newRole = request.Role?.Trim();
        if (string.IsNullOrWhiteSpace(newRole) || !Roles.All.Contains(newRole))
        {
            return Results.Problem(
                $"Invalid role '{newRole}'. Valid roles: {string.Join(", ", Roles.All)}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Remove from all roles, add the new one.
        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, newRole);
        user.UserType = Enum.Parse<Core.Domain.Enums.UserType>(newRole);
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return Results.Ok(ToResponse(user));
    }

    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> UpdateStatusAsync(
        string id,
        UpdateStatusRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.NotFound();

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return Results.Ok(ToResponse(user));
    }

    // --------------------------------------------------------------- Helpers

    private static UserResponse ToResponse(ApplicationUser u) => new(
        u.Id,
        u.FirstName,
        u.MiddleName,
        u.LastName,
        u.Email ?? string.Empty,
        u.UserType.ToString(),
        u.IsActive,
        u.EmailConfirmed,
        string.IsNullOrWhiteSpace(u.AvatarUrl) ? null : u.AvatarUrl,
        string.IsNullOrWhiteSpace(u.City) ? null : u.City,
        string.IsNullOrWhiteSpace(u.Country) ? null : u.Country,
        u.CreatedAt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"));

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
