using System.Text.Json;
using SeraGo.API.Responses;

namespace SeraGo.API.Middleware;

/// <summary>
/// Wraps every API response in the standard envelope
/// (<c>{ responseStatus, messageCode, message, data }</c>):
///
/// * 2xx JSON bodies are wrapped as <c>Success</c> (data = the original body,
///   or null when empty). 204/304 pass through untouched (no body).
/// * Error responses — RFC 7807 ProblemDetails, the rate limiter's custom 429,
///   empty 404s, framework 401s — are converted to the <c>Failed</c> envelope
///   with a status-derived <c>messageCode</c>. ProblemDetails
///   <c>title</c>/<c>detail</c>/field <c>errors</c> are flattened into
///   <c>message</c>.
/// * Unhandled exceptions become a 500 <c>Failed</c> envelope. In Development
///   the developer exception page runs first (it is added outermost by minimal
///   hosting), so the rich page is still what you see there.
///
/// Registered as the first middleware after Swagger — every endpoint,
/// including Aufy's auth endpoints, comes out in the one format.
/// </summary>
public class ResponseEnvelopeMiddleware
{
    // Same conventions the minimal APIs use (camelCase property names).
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseEnvelopeMiddleware> _logger;

    public ResponseEnvelopeMiddleware(RequestDelegate next, ILogger<ResponseEnvelopeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            context.Response.Body = originalBody;

            // Streaming / already-started responses can't be rewritten — flush
            // whatever was buffered so it still reaches the client.
            if (context.Response.HasStarted)
            {
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);
                return;
            }

            // Leave non-JSON bodies alone (the developer exception page's HTML
            // in Development, for example) — only JSON gets enveloped. The
            // buffered body must still be copied back to the real response.
            var contentType = context.Response.ContentType;
            if (!string.IsNullOrEmpty(contentType)
                && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);
                return;
            }

            var status = context.Response.StatusCode;

            // No body — nothing to envelope (DELETE 204, CORS preflight, ...).
            if (status is StatusCodes.Status204NoContent or StatusCodes.Status304NotModified)
            {
                return;
            }

            buffer.Position = 0;

            if (status >= 200 && status < 300)
            {
                await WriteSuccessAsync(context, originalBody, buffer);
            }
            else
            {
                await WriteFailureAsync(context, originalBody, buffer, status);
            }
        }
        catch (Exception ex)
        {
            context.Response.Body = originalBody;
            _logger.LogError(ex, "Unhandled exception while serving {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (!context.Response.HasStarted)
            {
                // Reset just the status/content, NOT all headers — CORS and
                // security headers set downstream must survive the 500 so the
                // browser can actually read the error.
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = null;
                context.Response.ContentLength = null;
                await WriteAsync(context, originalBody,
                    ApiResponses.Fail("INTERNAL_ERROR", "An unexpected error occurred."));
            }
        }
    }

    private async Task WriteSuccessAsync(HttpContext context, Stream body, Stream buffer)
    {
        object? data = null;
        if (buffer.Length > 0)
        {
            try
            {
                using var doc = JsonDocument.Parse(buffer);
                data = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // Non-JSON body — leave it exactly as the endpoint wrote it.
                buffer.Position = 0;
                await buffer.CopyToAsync(body);
                return;
            }
        }

        await WriteAsync(context, body, ApiResponses.Ok(data));
    }

    private async Task WriteFailureAsync(HttpContext context, Stream body, Stream buffer, int status)
    {
        var code = status switch
        {
            400 => "VALIDATION_ERROR",
            401 => "UNAUTHORIZED",
            403 => "FORBIDDEN",
            404 => "NOT_FOUND",
            405 => "METHOD_NOT_ALLOWED",
            409 => "CONFLICT",
            429 => "RATE_LIMITED",
            503 => "SERVICE_UNAVAILABLE",
            _ => "INTERNAL_ERROR",
        };

        var message = ReadErrorMessage(buffer);

        await WriteAsync(context, body,
            ApiResponses.Fail(code, string.IsNullOrWhiteSpace(message) ? $"Request failed ({status})." : message));
    }

    /// <summary>
    /// Pulls a human-readable message from a ProblemDetails / custom error body:
    /// our <c>message</c>, then ProblemDetails <c>detail</c> or <c>title</c>,
    /// then the flattened field-level <c>errors</c> dict.
    /// </summary>
    private static string? ReadErrorMessage(Stream buffer)
    {
        if (buffer.Length == 0)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(buffer);
            var root = doc.RootElement;

            var message = GetString(root, "message")
                ?? GetString(root, "detail")
                ?? GetString(root, "title");

            if (message is null
                && root.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Object)
            {
                var parts = new List<string>();
                foreach (var property in errors.EnumerateObject())
                {
                    foreach (var value in property.Value.EnumerateArray())
                    {
                        if (value.GetString() is { Length: > 0 } text)
                        {
                            parts.Add(text);
                        }
                    }
                }
                if (parts.Count > 0)
                {
                    message = string.Join(" ", parts);
                }
            }

            return message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private async Task WriteAsync(HttpContext context, Stream body, ApiResponse<object?> envelope)
    {
        context.Response.ContentType = "application/json";
        context.Response.ContentLength = null;
        await JsonSerializer.SerializeAsync(body, envelope, JsonSerializerOptions);
    }
}
