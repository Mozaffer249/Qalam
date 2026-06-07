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
    private readonly string _publicUrlPrefix;

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
            // Long-lived AccessKey path — dev laptop, or non-Aliyun host.
            _client = new OssClient(endpoint, _settings.AccessKeyId, _settings.AccessKeySecret, config);
            _logger.LogInformation("OSS client initialized with static AccessKey (bucket={Bucket})", _settings.BucketName);
        }
        else if (hasEcsRole)
        {
            // ECS RAM role — SDK fetches STS tokens from the instance metadata service
            // (http://100.100.100.200/...) and auto-rotates. No secret in env files.
            var credsProvider = new EcsRamRoleCredentialsProvider(_settings.EcsRoleName!, _logger);
            _client = new OssClient(endpoint, credsProvider, config);
            _logger.LogInformation("OSS client initialized with ECS RAM role '{Role}' (bucket={Bucket})",
                _settings.EcsRoleName, _settings.BucketName);
        }
        else
        {
            // Misconfigured — every upload will fail. Log loudly so it's obvious at startup.
            _logger.LogError("OSS is not configured: provide either OSS_ACCESS_KEY_ID + OSS_ACCESS_KEY_SECRET, " +
                             "or OSS_ECS_ROLE_NAME for ECS deployments. Uploads will fail.");
            _client = new OssClient(endpoint, string.Empty, string.Empty, config);
        }

        if (!string.IsNullOrWhiteSpace(_settings.Region))
            _client.SetRegion(_settings.Region);

        _publicUrlPrefix = $"{_settings.PublicBaseUrl.TrimEnd('/')}/";
    }

    public Task<string> UploadFileAsync(string key, Stream stream, string contentType)
    {
        try
        {
            var metadata = new ObjectMetadata();
            if (!string.IsNullOrWhiteSpace(contentType))
                metadata.ContentType = contentType;

            _client.PutObject(_settings.BucketName, key, stream, metadata);

            var fileUrl = $"{_publicUrlPrefix}{key}";
            _logger.LogInformation("File uploaded to OSS: {Key} → {Url}", key, fileUrl);
            return Task.FromResult(fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to OSS: {Key}", key);
            throw;
        }
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            var key = ExtractKeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(key))
                return Task.CompletedTask;

            _client.DeleteObject(_settings.BucketName, key);
            _logger.LogInformation("File deleted from OSS: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from OSS: {Url}", fileUrl);
            throw;
        }
    }

    internal string? ExtractKeyFromUrl(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return null;

        if (fileUrl.StartsWith(_publicUrlPrefix, StringComparison.OrdinalIgnoreCase))
            return fileUrl[_publicUrlPrefix.Length..];

        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            // Virtual-hosted: https://bucket.oss-region.aliyuncs.com/key
            if (uri.Host.StartsWith($"{_settings.BucketName}.", StringComparison.OrdinalIgnoreCase))
            {
                var path = uri.AbsolutePath.TrimStart('/');
                return string.IsNullOrEmpty(path) ? null : path;
            }

            // Path-style Wasabi legacy: https://s3.region.wasabisys.com/bucket/key
            if (uri.Host.Contains("wasabisys.com", StringComparison.OrdinalIgnoreCase))
            {
                var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 2)
                    return segments[1];
            }

            // Path-style OSS: https://oss-region.aliyuncs.com/bucket/key
            if (uri.Host.Contains("aliyuncs.com", StringComparison.OrdinalIgnoreCase))
            {
                var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 2 && segments[0].Equals(_settings.BucketName, StringComparison.OrdinalIgnoreCase))
                    return segments[1];
            }
        }

        if (!fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return fileUrl.TrimStart('/');

        return null;
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
