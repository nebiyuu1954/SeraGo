using Amazon.S3;
using Amazon.S3.Model;

namespace SeraGo.API.Services;

/// <summary>
/// Thrown when a storage operation is attempted but the Cloudflare R2
/// environment variables are not configured. Endpoints catch THIS type
/// specifically (never a bare <c>Exception</c>) and translate it to a 503 —
/// a blanket catch would also swallow real AWS/network misconfigurations
/// that deserve their own error surface.
/// </summary>
public sealed class R2NotConfiguredException : Exception
{
    public R2NotConfiguredException(string message) : base(message) { }
}

/// <summary>
/// Generates presigned URLs for Cloudflare R2 (S3-compatible) file uploads and downloads.
/// Profile pictures go to a public bucket, resumes to a private bucket.
///
/// Initialization is LAZY: the constructor never throws. R2 env vars may
/// legitimately be absent (e.g. a deploy without file storage) and the rest
/// of the app must keep working — registration as a singleton must not
/// depend on them. Every storage method calls <see cref="EnsureReady"/>
/// first, which throws <see cref="R2NotConfiguredException"/> when
/// configuration is missing; FileUploadEndpoints translates that to a 503.
/// </summary>
public class R2StorageService
{
    private readonly string? _accessKey;
    private readonly string? _secretKey;
    private readonly string? _accountId;
    private readonly string? _bucketName;
    private readonly string? _publicUrl;

    /// <summary>Names of the R2 env vars that were absent at construction.</summary>
    private readonly string[] _missing;

    private IAmazonS3? _s3Client;

    public R2StorageService()
    {
        _accessKey = Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID");
        _secretKey = Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY");
        _accountId = Environment.GetEnvironmentVariable("CLOUDFLARE_ACCOUNT_ID");
        _bucketName = Environment.GetEnvironmentVariable("R2_BUCKET_NAME");
        _publicUrl = Environment.GetEnvironmentVariable("R2_PUBLIC_URL");

        _missing =
        [
            .. string.IsNullOrWhiteSpace(_accessKey) ? new[] { "R2_ACCESS_KEY_ID" } : Array.Empty<string>(),
            .. string.IsNullOrWhiteSpace(_secretKey) ? new[] { "R2_SECRET_ACCESS_KEY" } : Array.Empty<string>(),
            .. string.IsNullOrWhiteSpace(_accountId) ? new[] { "CLOUDFLARE_ACCOUNT_ID" } : Array.Empty<string>(),
            .. string.IsNullOrWhiteSpace(_bucketName) ? new[] { "R2_BUCKET_NAME" } : Array.Empty<string>(),
            .. string.IsNullOrWhiteSpace(_publicUrl) ? new[] { "R2_PUBLIC_URL" } : Array.Empty<string>(),
        ];
    }

    /// <summary>True when all five R2 environment variables are present.</summary>
    public bool IsConfigured => _missing.Length == 0;

    /// <summary>
    /// Guarantees configuration + client. Throws
    /// <see cref="R2NotConfiguredException"/> — caught ONLY by the upload
    /// endpoints (never a generic catch) so a genuine AWS error can't be
    /// masked as "not configured".
    /// </summary>
    private void EnsureReady()
    {
        if (_missing.Length > 0)
        {
            throw new R2NotConfiguredException(
                "File storage (Cloudflare R2) is not configured on this server. "
                + "Missing environment variables: " + string.Join(", ", _missing) + ".");
        }

        _s3Client ??= new AmazonS3Client(
            _accessKey,
            _secretKey,
            new AmazonS3Config
            {
                ServiceURL = $"https://{_accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
                // Do NOT set RegionEndpoint — it overrides ServiceURL
                // and generates URLs pointing to s3.amazonaws.com instead of R2.
            });
    }

    /// <summary>
    /// Generate a presigned URL for uploading a file directly to R2.
    /// The frontend uses this URL to PUT the file, bypassing the server.
    /// </summary>
    /// <param name="userId">The user's ID (used in the file path)</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME type (e.g., image/jpeg, application/pdf)</param>
    /// <param name="folder">Subfolder: "avatars" or "resumes"</param>
    /// <returns>Presigned upload URL and the final public URL</returns>
    public async Task<PresignedUrlResponse> GetPresignedUploadUrlAsync(
        string userId, string fileName, string contentType, string folder)
    {
        EnsureReady();

        // Sanitize file name and add timestamp to avoid conflicts
        var cleanName = Path.GetFileName(fileName).Replace(" ", "_");
        var key = $"{folder}/{userId}/{DateTime.UtcNow:yyyyMMdd_HHmmss}_{cleanName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(5),
            // Do NOT set ContentType — it's included in the signature,
            // and HttpClient may send it differently, causing a 401.
        };

        var url = await _s3Client!.GetPreSignedURLAsync(request);

        // For avatars (public), use the public URL. For resumes (private), use the presigned URL.
        var publicUrl = folder == "avatars"
            ? $"{_publicUrl}/{key}"
            : null;

        return new PresignedUrlResponse(url, publicUrl, key);
    }

    /// <summary>
    /// Generate a SigV4 presigned URL for downloading a private file (e.g., resumes).
    /// Cloudflare R2 requires SigV4 — the AWS SDK default (SigV2) is not supported.
    /// Valid for 15 minutes.
    /// </summary>
    public string GetPresignedDownloadUrlAsync(string key)
    {
        EnsureReady();

        var expiresIn = 900; // 15 minutes
        var date = DateTime.UtcNow;
        var dateStamp = date.ToString("yyyyMMdd");
        var amzDate = date.ToString("yyyyMMdd'T'HHmmss'Z'");
        var endpoint = $"https://{_accountId}.r2.cloudflarestorage.com";
        var canonicalUri = string.Join("/", $"/{_bucketName}/{key}".Split('/').Select(Uri.EscapeDataString));
        var payloadHash = "UNSIGNED-PAYLOAD";

        // Credential scope
        var credentialScope = $"{dateStamp}/auto/s3/aws4_request";

        // Canonical query string
        var queryParams = new SortedDictionary<string, string>
        {
            { "X-Amz-Algorithm", "AWS4-HMAC-SHA256" },
            { "X-Amz-Credential", $"{_accessKey}/{credentialScope}" },
            { "X-Amz-Date", amzDate },
            { "X-Amz-Expires", expiresIn.ToString() },
            { "X-Amz-SignedHeaders", "host" },
        };
        var canonicalQueryString = string.Join("&", queryParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        // Canonical request
        var canonicalRequest = $"GET\n{canonicalUri}\n{canonicalQueryString}\nhost:{_accountId}.r2.cloudflarestorage.com\n\nhost\n{payloadHash}";

        // String to sign
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{ToHexLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonicalRequest)))}";

        // Signing key
        var kDate = HmacSha256(System.Text.Encoding.UTF8.GetBytes($"AWS4{_secretKey}"), dateStamp);
        var kRegion = HmacSha256(kDate, "auto");
        var kService = HmacSha256(kRegion, "s3");
        var kSigning = HmacSha256(kService, "aws4_request");

        var signature = ToHexLower(
            HmacSha256(kSigning, stringToSign));

        return $"{endpoint}{canonicalUri}?{canonicalQueryString}&X-Amz-Signature={signature}";
    }

    /// <summary>
    /// Delete a file from R2 (used when user replaces their avatar or resume).
    /// </summary>
    public async Task DeleteFileAsync(string key)
    {
        EnsureReady();

        await _s3Client!.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
        });
    }

    /// <summary>
    /// Download a file from R2 via raw HTTP GET with SigV4 signing.
    /// Returns (stream, contentType, fileName). Caller must dispose the stream.
    /// </summary>
    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadFileAsync(string key)
    {
        EnsureReady();

        var date = DateTime.UtcNow;
        var dateStamp = date.ToString("yyyyMMdd");
        var amzDate = date.ToString("yyyyMMdd'T'HHmmss'Z'");
        var endpoint = $"https://{_accountId}.r2.cloudflarestorage.com";
        var canonicalUri = string.Join("/", $"/{_bucketName}/{key}".Split('/').Select(Uri.EscapeDataString));
        var payloadHash = "UNSIGNED-PAYLOAD";

        var signedHeaders = "host";
        var canonicalHeaders = $"host:{_accountId}.r2.cloudflarestorage.com\n";

        var canonicalRequest = $"GET\n{canonicalUri}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        var credentialScope = $"{dateStamp}/auto/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{ToHexLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonicalRequest)))}";

        var kDate = HmacSha256(System.Text.Encoding.UTF8.GetBytes($"AWS4{_secretKey}"), dateStamp);
        var kRegion = HmacSha256(kDate, "auto");
        var kService = HmacSha256(kRegion, "s3");
        var kSigning = HmacSha256(kService, "aws4_request");
        var signature = ToHexLower(HmacSha256(kSigning, stringToSign));

        var url = $"{endpoint}{canonicalUri}";
        using var http = new HttpClient();
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        req.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        req.Headers.TryAddWithoutValidation("Authorization", $"AWS4-HMAC-SHA256 Credential={_accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");

        var response = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = Path.GetFileName(key);
        var stream = await response.Content.ReadAsStreamAsync();
        return (stream, contentType, fileName);
    }

    /// <summary>
    /// Upload a file to R2 via raw HTTP PUT (bypasses the SDK chunked-encoding issue).
    /// Uses AWS4 signing manually.
    /// </summary>
    public async Task<string> UploadFileAsync(string userId, string fileName, string contentType, string folder, Stream fileStream)
    {
        EnsureReady();

        var cleanName = Path.GetFileName(fileName).Replace(" ", "_");
        var key = $"{folder}/{userId}/{DateTime.UtcNow:yyyyMMdd_HHmmss}_{cleanName}";

        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        var bodyBytes = ms.ToArray();

        var date = DateTime.UtcNow;
        var dateStamp = date.ToString("yyyyMMdd");
        var amzDate = date.ToString("yyyyMMdd'T'HHmmss'Z'");
        var endpoint = $"https://{_accountId}.r2.cloudflarestorage.com";
        var canonicalUri = string.Join("/", $"/{_bucketName}/{key}".Split('/').Select(Uri.EscapeDataString));

        // SHA256 of body
        var payloadHash = ToHexLower(
            System.Security.Cryptography.SHA256.HashData(bodyBytes));

        // Canonical headers
        var signedHeaders = "content-type;host;x-amz-content-sha256;x-amz-date";
        var canonicalHeaders = $"content-type:{contentType}\nhost:{_accountId}.r2.cloudflarestorage.com\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";

        // Canonical request
        var canonicalRequest = $"PUT\n{canonicalUri}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        // String to sign
        var credentialScope = $"{dateStamp}/auto/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{ToHexLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonicalRequest)))}";

        // Signing key
        var kDate = HmacSha256(System.Text.Encoding.UTF8.GetBytes($"AWS4{_secretKey}"), dateStamp);
        var kRegion = HmacSha256(kDate, "auto");
        var kService = HmacSha256(kRegion, "s3");
        var kSigning = HmacSha256(kService, "aws4_request");

        var signature = ToHexLower(
            HmacSha256(kSigning, stringToSign));

        var authorization = $"AWS4-HMAC-SHA256 Credential={_accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        var url = $"{endpoint}{canonicalUri}";
        using var http = new HttpClient();
        var req = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new ByteArrayContent(bodyBytes),
        };
        req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        req.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        req.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        req.Headers.TryAddWithoutValidation("Authorization", authorization);

        var response = await http.SendAsync(req);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"R2 PUT failed ({response.StatusCode}): {errBody}");
        }

        return folder == "avatars"
            ? $"{_publicUrl}/{key}"
            : key;
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        return hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
    }

    private static string ToHexLower(byte[] bytes) =>
        BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

    public record PresignedUrlResponse(string UploadUrl, string? PublicUrl, string Key);
}
