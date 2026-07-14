using Aliyun.OSS;
using Aliyun.OSS.Common;
using Aliyun.OSS.Common.Authentication;
using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Services;

public class OssStorageService : IObjectStorageService
{
    private readonly OssSettings _settings;
    private readonly ILogger<OssStorageService> _logger;
    private readonly OssClient _client;

    public OssStorageService(IOptions<OssSettings> settings, ILogger<OssStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var endpoint = NormalizeEndpoint(_settings.Endpoint);
        var config = new ClientConfiguration
        {
            SignatureVersion = SignatureVersion.V4
        };

        var hasStaticCreds = !string.IsNullOrWhiteSpace(_settings.AccessKeyId)
                          && !string.IsNullOrWhiteSpace(_settings.AccessKeySecret);
        var hasEcsRole = !string.IsNullOrWhiteSpace(_settings.EcsRoleName);

        if (hasStaticCreds)
        {
            _client = new OssClient(endpoint, _settings.AccessKeyId, _settings.AccessKeySecret, config);
            _logger.LogInformation(
                "OSS client initialized with static AccessKey (identities={Identities}, learning={Learning})",
                _settings.Resolve(OssBucketKeys.Identities).BucketName,
                _settings.Resolve(OssBucketKeys.Learning).BucketName);
        }
        else if (hasEcsRole)
        {
            var credsProvider = new EcsRamRoleCredentialsProvider(_settings.EcsRoleName!, _logger);
            _client = new OssClient(endpoint, credsProvider, config);
            _logger.LogInformation(
                "OSS client initialized with ECS RAM role '{Role}' (identities={Identities}, learning={Learning})",
                _settings.EcsRoleName,
                _settings.Resolve(OssBucketKeys.Identities).BucketName,
                _settings.Resolve(OssBucketKeys.Learning).BucketName);
        }
        else
        {
            _logger.LogError("OSS is not configured: provide either OSS_ACCESS_KEY_ID + OSS_ACCESS_KEY_SECRET, " +
                             "or OSS_ECS_ROLE_NAME for ECS deployments. Uploads will fail.");
            _client = new OssClient(endpoint, string.Empty, string.Empty, config);
        }

        if (!string.IsNullOrWhiteSpace(_settings.Region))
            _client.SetRegion(_settings.Region);
    }

    public Task<string> UploadFileAsync(string key, Stream stream, string contentType)
        => UploadFileAsync(key, stream, contentType, OssBucketKeys.Identities);

    public Task<string> UploadFileAsync(string key, Stream stream, string contentType, string bucketKey)
    {
        try
        {
            var endpoint = _settings.Resolve(bucketKey);
            var metadata = new ObjectMetadata();
            if (!string.IsNullOrWhiteSpace(contentType))
                metadata.ContentType = contentType;

            _client.PutObject(endpoint.BucketName, key, stream, metadata);

            var baseUrl = endpoint.PublicBaseUrl.TrimEnd('/');
            var fileUrl = $"{baseUrl}/{key}";
            _logger.LogInformation("File uploaded to OSS bucket={Bucket} key={Key} → {Url}",
                endpoint.BucketName, key, fileUrl);
            return Task.FromResult(fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to OSS ({BucketKey}): {Key}", bucketKey, key);
            throw;
        }
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            var (bucketName, key) = ResolveBucketAndKey(fileUrl);
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(bucketName))
                return Task.CompletedTask;

            _client.DeleteObject(bucketName, key);
            _logger.LogInformation("File deleted from OSS: bucket={Bucket} key={Key}", bucketName, key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from OSS: {Url}", fileUrl);
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

        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var absolute))
        {
            if (absolute.Host.Contains("wasabisys.com", StringComparison.OrdinalIgnoreCase))
            {
                var segments = absolute.AbsolutePath.TrimStart('/').Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 2)
                    return (segments[0], segments[1]);
            }

            if (absolute.Host.Contains("aliyuncs.com", StringComparison.OrdinalIgnoreCase))
            {
                var segments = absolute.AbsolutePath.TrimStart('/').Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 2)
                    return (segments[0], segments[1]);
            }
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
        var value = endpoint.Trim();
        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            value = "https://" + value;
        return value.TrimEnd('/');
    }
}
