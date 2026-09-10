using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Services;

public class WasabiStorageService : IObjectStorageService, IDisposable
{
    private readonly WasabiSettings _settings;
    private readonly ILogger<WasabiStorageService> _logger;
    private readonly IAmazonS3 _client;

    public WasabiStorageService(IOptions<WasabiSettings> settings, ILogger<WasabiStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var endpoint = NormalizeEndpoint(_settings.Endpoint);
        var region = string.IsNullOrWhiteSpace(_settings.Region) ? "ap-southeast-1" : _settings.Region.Trim();

        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = false,
            AuthenticationRegion = region,
        };

        if (!string.IsNullOrWhiteSpace(_settings.AccessKeyId)
            && !string.IsNullOrWhiteSpace(_settings.AccessKeySecret))
        {
            var credentials = new BasicAWSCredentials(_settings.AccessKeyId, _settings.AccessKeySecret);
            _client = new AmazonS3Client(credentials, config);
            _logger.LogInformation(
                "Wasabi S3 client initialized (identities={Identities}, learning={Learning}, endpoint={Endpoint})",
                _settings.Resolve(OssBucketKeys.Identities).BucketName,
                _settings.Resolve(OssBucketKeys.Learning).BucketName,
                endpoint);
        }
        else
        {
            _logger.LogError(
                "Wasabi is not configured: provide WASABI_ACCESS_KEY_ID + WASABI_ACCESS_KEY_SECRET. Uploads will fail.");
            _client = new AmazonS3Client(new BasicAWSCredentials("missing", "missing"), config);
        }
    }

    public Task<string> UploadFileAsync(string key, Stream stream, string contentType)
        => UploadFileAsync(key, stream, contentType, OssBucketKeys.Identities);

    public async Task<string> UploadFileAsync(string key, Stream stream, string contentType, string bucketKey)
    {
        try
        {
            var endpoint = _settings.Resolve(bucketKey);
            var request = new PutObjectRequest
            {
                BucketName = endpoint.BucketName,
                Key = key,
                InputStream = stream,
                AutoCloseStream = false,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            };

            await _client.PutObjectAsync(request);

            var baseUrl = endpoint.PublicBaseUrl.TrimEnd('/');
            var fileUrl = $"{baseUrl}/{key}";
            _logger.LogInformation("File uploaded to Wasabi bucket={Bucket} key={Key} → {Url}",
                endpoint.BucketName, key, fileUrl);
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Wasabi ({BucketKey}): {Key}", bucketKey, key);
            throw;
        }
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            var (bucketName, key) = ResolveBucketAndKey(fileUrl);
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(bucketName))
                return;

            await _client.DeleteObjectAsync(bucketName, key);
            _logger.LogInformation("File deleted from Wasabi: bucket={Bucket} key={Key}", bucketName, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from Wasabi: {Url}", fileUrl);
            throw;
        }
    }

    private (string? BucketName, string? Key) ResolveBucketAndKey(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return (null, null);

        foreach (var key in new[] { OssBucketKeys.Learning, OssBucketKeys.Identities })
        {
            OssBucketEndpoint endpoint;
            try { endpoint = _settings.Resolve(key); }
            catch { continue; }

            var prefix = endpoint.PublicBaseUrl.TrimEnd('/') + "/";
            if (fileUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return (endpoint.BucketName, fileUrl[prefix.Length..]);

            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri)
                && uri.Host.StartsWith($"{endpoint.BucketName}.", StringComparison.OrdinalIgnoreCase))
            {
                var path = uri.AbsolutePath.TrimStart('/');
                if (!string.IsNullOrEmpty(path))
                    return (endpoint.BucketName, path);
            }
        }

        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var absolute)
            && absolute.Host.Contains("wasabisys.com", StringComparison.OrdinalIgnoreCase))
        {
            // Virtual-hosted: bucket.s3.region.wasabisys.com/key
            var hostParts = absolute.Host.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
            if (hostParts.Length >= 2
                && !hostParts[0].Equals("s3", StringComparison.OrdinalIgnoreCase))
            {
                var path = absolute.AbsolutePath.TrimStart('/');
                if (!string.IsNullOrEmpty(path))
                    return (hostParts[0], path);
            }

            // Path-style: s3.region.wasabisys.com/bucket/key
            var segments = absolute.AbsolutePath.TrimStart('/').Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 2)
                return (segments[0], segments[1]);
        }

        if (!fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var identities = _settings.Resolve(OssBucketKeys.Identities);
            return (identities.BucketName, fileUrl.TrimStart('/'));
        }

        return (null, null);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        var value = (endpoint ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(value))
            value = "https://s3.ap-southeast-1.wasabisys.com";
        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            value = "https://" + value;
        return value.TrimEnd('/');
    }

    public void Dispose() => _client.Dispose();
}
