namespace SeraGo.API.Responses;

/// <summary>Values for <see cref="ApiResponse{T}.ResponseStatus"/>.</summary>
public static class ApiResponseStatus
{
    public const string Success = "Success";
    public const string Failed = "Failed";
}

/// <summary>
/// The standard envelope every SeraGo API response uses. Produced by
/// <c>ResponseEnvelopeMiddleware</c> — endpoints return their plain DTOs or
/// errors and the middleware wraps them:
///
///   success → { responseStatus: "Success", messageCode: null, message: null, data: … }
///   failure → { responseStatus: "Failed",  messageCode: "…",     message: "…",   data: null }
///
/// <see cref="MessageCode"/> is a stable, machine-readable code the frontend
/// can switch on (or map to localized text); <see cref="Message"/> is the
/// human-readable fallback.
/// </summary>
public sealed record ApiResponse<T>(
    string ResponseStatus,
    string? MessageCode,
    string? Message,
    T? Data);

/// <summary>Factories for the standard envelope.</summary>
public static class ApiResponses
{
    public static ApiResponse<T> Ok<T>(T data) =>
        new(ApiResponseStatus.Success, null, null, data);

    public static ApiResponse<object?> Fail(string messageCode, string message) =>
        new(ApiResponseStatus.Failed, messageCode, message, null);
}
