using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SeraGo.API.Endpoints;

/// <summary>
/// HTTP caching helpers for public, read-only GET endpoints (the job feed,
/// job locations, sectors). These responses are identical for every caller,
/// so the browser may store them (<c>Cache-Control: public, max-age</c>) and
/// re-ask conditionally via <c>If-None-Match</c>; when nothing changed the
/// endpoint answers <c>304 Not Modified</c> with an empty body instead of
/// re-sending the payload.
///
/// Only endpoints whose response does NOT depend on the caller should use
/// this. Per-user data (saved jobs, applications, the personalized "For You"
/// feed, admin/owner scopes) and endpoints with side effects (job detail
/// increments view counts) must stay uncached.
/// </summary>
public static class HttpCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Strong ETag for a response payload: a SHA-256 over its JSON.</summary>
    public static string ComputeEtag(object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return $"\"{Convert.ToHexString(hash).ToLowerInvariant()}\"";
    }

    /// <summary>True when the request's If-None-Match header carries the etag.</summary>
    public static bool IsNotModified(HttpRequest request, string etag)
    {
        foreach (var value in request.Headers.IfNoneMatch)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }
            foreach (var candidate in value.Split(','))
            {
                if (candidate.Trim().Equals(etag, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>Label the response as publicly cacheable and stamp its validator.</summary>
    public static void Apply(HttpResponse response, string etag, TimeSpan maxAge)
    {
        response.Headers.ETag = etag;
        response.Headers.CacheControl = $"public, max-age={(int)maxAge.TotalSeconds}";
    }

    /// <summary>Empty 304 — the envelope middleware lets these pass through untouched.</summary>
    public static IResult NotModified() => Results.StatusCode(StatusCodes.Status304NotModified);
}
