using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Aufy.Core.EmailSender;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeraGo.API.Email;
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
///   POST   /api/admin/users/{id}/send-reset-email — trigger password reset for target
///   PATCH  /api/admin/users/{id}/anonymize — deactivate + strip PII
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
        group.MapPost("/{id}/send-reset-email", SendResetEmailAsync).WithOpenApi();
        group.MapPatch("/{id}/anonymize", AnonymizeUserAsync).WithOpenApi();

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

    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> SendResetEmailAsync(
        string id,
        UserManager<ApplicationUser> userManager,
        IAufyEmailSenderManager<ApplicationUser> emailSender,
        IOptions<IdentityOptions> identityOptions,
        IOptions<Aufy.Core.AufyOptions> aufyOptions,
        ILoggerFactory loggerFactory,
        EmailThrottleService throttle,
        HttpRequest httpRequest)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.NotFound();

        if (identityOptions.Value.SignIn.RequireConfirmedEmail && user is not { EmailConfirmed: true })
        {
            return Results.Problem(
                "Cannot send reset email to a user with an unconfirmed email.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!throttle.TryAllow(user.Email!, out var retryAfter))
        {
            return Results.Problem(retryAfter, statusCode: StatusCodes.Status429TooManyRequests);
        }

        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var baseUri = new Uri(
            new Uri(aufyOptions.Value.ClientApp.BaseUrl ?? $"{httpRequest.Scheme}://{httpRequest.Host}"),
            aufyOptions.Value.ClientApp.PasswordResetPath);
        var link = new Uri(baseUri, $"?code={code}");
        await emailSender.SendPasswordForgotAsync(user, link.ToString());

        var logger = loggerFactory.CreateLogger("SeraGo.AdminUserEndpoints");
        logger.LogInformation("Admin triggered password reset for {Email}", user.Email);
        return Results.Ok();
    }

    [Authorize(Roles = Roles.Admin)]
    private static async Task<IResult> AnonymizeUserAsync(
        string id,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.NotFound();

        // Deactivate
        user.IsActive = false;

        // Strip PII — keep the row for stats/audit
        user.FirstName = "Deleted";
        user.MiddleName = string.Empty;
        user.LastName = "User";
        user.AvatarUrl = null;
        user.City = null;
        user.Country = null;

        // Email must stay unique in Identity — use a guaranteed-unique placeholder
        var anonEmail = $"deleted-{user.Id[..8]}@anon.serago";
        user.Email = anonEmail;
        user.NormalizedEmail = anonEmail.ToUpperInvariant();
        user.UserName = anonEmail;
        user.NormalizedUserName = anonEmail.ToUpperInvariant();

        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Strip PII from talent profile if it exists
        var talentProfile = await db.TalentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (talentProfile is not null)
        {
            talentProfile.Headline = string.Empty;
            talentProfile.About = string.Empty;
            talentProfile.Skills = [];
            talentProfile.DesiredRoles = [];
            talentProfile.DesiredJobTypes = [];
            talentProfile.ResumeUrl = string.Empty;
            talentProfile.LinkedInUrl = string.Empty;
            talentProfile.GitHubUrl = string.Empty;
            talentProfile.PortfolioUrl = string.Empty;
            talentProfile.Address = string.Empty;
            talentProfile.PreferredLocations = "[]";
            talentProfile.WorkExperience = "[]";
            talentProfile.EducationHistory = "[]";
            talentProfile.CurrentIndustry = string.Empty;
            talentProfile.CurrentProfession = string.Empty;
            await db.SaveChangesAsync();
        }

        // Strip PII from recruiter profile if it exists
        var recruiterProfile = await db.RecruiterProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (recruiterProfile is not null)
        {
            recruiterProfile.CompanyName = "Deleted Company";
            recruiterProfile.WebsiteUrl = string.Empty;
            recruiterProfile.About = string.Empty;
            recruiterProfile.Headquarters = string.Empty;
            recruiterProfile.PhoneNumber = string.Empty;
            recruiterProfile.Email = anonEmail;
            recruiterProfile.LinkedInUrl = string.Empty;
            recruiterProfile.TwitterUrl = string.Empty;
            await db.SaveChangesAsync();
        }

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
