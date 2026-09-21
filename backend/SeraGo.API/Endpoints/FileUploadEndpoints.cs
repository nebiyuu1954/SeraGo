using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeraGo.Core.Domain.Entities;
using SeraGo.API.Services;

namespace SeraGo.API.Endpoints;

/// <summary>
/// File upload API — generates presigned URLs for direct R2 uploads.
/// 
/// POST /api/upload/presign       — get a presigned PUT URL for uploading a file
/// POST /api/upload/presign-get   — get a presigned GET URL for downloading a private file
/// 
/// The frontend uploads directly to R2 using the presigned URL, bypassing the server.
/// This keeps server bandwidth low and allows large file uploads.
/// </summary>
public static class FileUploadEndpoints
{
    public static IEndpointRouteBuilder MapFileUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/upload").WithTags("File Upload");        group.MapPost("/file", UploadFileAsync)
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/presign", GetPresignedUploadUrlAsync)
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/download", DownloadFileAsync)
            .RequireAuthorization()
            .WithOpenApi();

        return app;
    }

    // ---------------------------------------------------------------- DTOs

    public sealed class PresignUploadRequest
    {
        public string fileName { get; set; } = string.Empty;
        public string contentType { get; set; } = string.Empty;
        public string fileType { get; set; } = string.Empty; // "avatar" or "resume"
    }

    public sealed class PresignDownloadRequest
    {
        public string key { get; set; } = string.Empty;
    }

    public sealed record PresignedUrlResponse(
        string UploadUrl,
        string? PublicUrl,
        string Key
    );

    // ------------------------------------------------------------- Handlers

    /// <summary>
    /// POST /api/upload/file — upload a file directly through the server.
    /// Avoids CORS issues with R2. Accepts multipart/form-data.
    /// </summary>
    [Authorize]
    private static async Task<IResult> UploadFileAsync(
        HttpContext httpContext,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        R2StorageService storageService)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null) return Results.Unauthorized();

        var form = await httpContext.Request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        var fileType = form["fileType"].FirstOrDefault() ?? "avatar";

        if (file is null || file.Length == 0)
            return Results.Problem("No file uploaded.", statusCode: StatusCodes.Status400BadRequest);

        // Validate file type
        var allowedTypes = fileType switch
        {
            "avatar" => new[] { "image/jpeg", "image/png", "image/webp", "image/gif" },
            "resume" => new[] { "application/pdf" },
            _ => Array.Empty<string>()
        };
        if (allowedTypes.Length == 0 || !allowedTypes.Contains(file.ContentType?.ToLowerInvariant() ?? ""))
            return Results.Problem($"Invalid file type.", statusCode: StatusCodes.Status400BadRequest);

        // Max size: 5MB for avatars, 10MB for resumes
        var maxSize = fileType == "avatar" ? 5 * 1024 * 1024L : 10 * 1024 * 1024L;
        if (file.Length > maxSize)
            return Results.Problem($"File too large. Max {maxSize / 1024 / 1024}MB.", statusCode: StatusCodes.Status400BadRequest);

        var folder = fileType == "avatar" ? "avatars" : "resumes";
        using var stream = file.OpenReadStream();
        try
        {
            var url = await storageService.UploadFileAsync(user.Id, file.FileName, file.ContentType!, folder, stream);
            return Results.Ok(new { Url = url });
        }
        catch (R2NotConfiguredException)
        {
            // Deliberately narrow: ONLY the not-configured case becomes a 503.
            // A blanket catch here would hide real AWS/network misconfigurations
            // (bad credentials, wrong bucket, DNS failures) as "not configured".
            return Results.Problem(
                "File uploads are not configured on this server.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// POST /api/upload/presign — generate a presigned URL for uploading a file.
    /// Validates file type and size before generating the URL.
    /// </summary>
    [Authorize]
    private static async Task<IResult> GetPresignedUploadUrlAsync(
        HttpContext httpContext,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        R2StorageService storageService)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        // Read body BEFORE any middleware touches it
        httpContext.Request.EnableBuffering();
        using var ms = new MemoryStream();
        await httpContext.Request.Body.CopyToAsync(ms);
        ms.Position = 0;
        var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        httpContext.Request.Body.Position = 0;

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        var fileName = root.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() ?? string.Empty : string.Empty;
        var contentType = root.TryGetProperty("contentType", out var ctProp) ? ctProp.GetString() ?? string.Empty : string.Empty;
        var fileType = root.TryGetProperty("fileType", out var ftProp) ? ftProp.GetString() ?? string.Empty : string.Empty;

        // Validate file type
        var allowedTypes = fileType switch
        {
            "avatar" => new[] { "image/jpeg", "image/png", "image/webp", "image/gif" },
            "resume" => new[] { "application/pdf" },
            _ => Array.Empty<string>()
        };

        if (allowedTypes.Length == 0)
        {
            return Results.Problem(
                "Invalid file type. Allowed: avatar (image) or resume (PDF).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var ctLower = contentType.ToLowerInvariant();
        if (!allowedTypes.Contains(ctLower))
        {
            return Results.Problem(
                $"Invalid content type '{contentType}'. Allowed: {string.Join(", ", allowedTypes)}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Validate file name
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Results.Problem(
                "File name is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Determine folder
        var folder = fileType == "avatar" ? "avatars" : "resumes";

        try
        {
            var result = await storageService.GetPresignedUploadUrlAsync(
                user.Id, fileName, contentType, folder);

            return Results.Ok(new PresignedUrlResponse(
                result.UploadUrl,
                result.PublicUrl,
                result.Key));
        }
        catch (R2NotConfiguredException)
        {
            return Results.Problem(
                "File uploads are not configured on this server.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                $"Failed to generate upload URL: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// POST /api/upload/download — download a file from R2 and stream it to the client.
    /// Only allows users to download their own files.
    /// </summary>
    [Authorize]
    private static async Task<IResult> DownloadFileAsync(
        HttpContext httpContext,
        ClaimsPrincipal claims,
        UserManager<ApplicationUser> userManager,
        R2StorageService storageService)
    {
        var user = await userManager.GetUserAsync(claims);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        httpContext.Request.EnableBuffering();
        using var downloadMs = new MemoryStream();
        await httpContext.Request.Body.CopyToAsync(downloadMs);
        downloadMs.Position = 0;
        var downloadJson = System.Text.Encoding.UTF8.GetString(downloadMs.ToArray());
        httpContext.Request.Body.Position = 0;

        var downloadDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(downloadJson);
        var key = downloadDict?.TryGetValue("key", out var k) == true ? k : string.Empty;

        if (string.IsNullOrWhiteSpace(key))
        {
            return Results.Problem("File key is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        // Security: only allow authenticated users to download files
        // Resumes are shared during applications, avatars are public
        if (!key.StartsWith("resumes/", StringComparison.OrdinalIgnoreCase) && !key.StartsWith("avatars/", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                "You can only download your own files.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        try
        {
            var url = storageService.GetPresignedDownloadUrlAsync(key);
            return Results.Ok(new { downloadUrl = url });
        }
        catch (R2NotConfiguredException)
        {
            return Results.Problem(
                "File uploads are not configured on this server.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                $"Failed to generate download URL: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
