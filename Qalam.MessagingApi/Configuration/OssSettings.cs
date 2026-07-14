namespace Qalam.MessagingApi.Configuration;

/// <summary>
/// Logical OSS bucket domains. Add a new constant + env entry when introducing a physical bucket.
/// </summary>
public static class OssBucketKeys
{
    public const string Identities = "identities";
    public const string Learning = "learning";
    // Future: public const string Chat = "chat";
}

public class OssBucketEndpoint
{
    public string BucketName { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
}

public class OssSettings
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string Region { get; set; } = "me-central-1";
    /// <summary>API endpoint for uploads (public or internal, without bucket in path).</summary>
    public string Endpoint { get; set; } = "https://oss-me-central-1.aliyuncs.com";
    public bool UseInternalEndpoint { get; set; }
    /// <summary>
    /// When set AND AccessKeyId/Secret are blank, the SDK fetches short-lived STS tokens from
    /// the ECS instance metadata service for this role. Used on Aliyun ECS (staging/prod) to
    /// avoid long-lived AccessKeys in env files. Leave empty for local dev (laptop, not on ECS).
    /// </summary>
    public string? EcsRoleName { get; set; }

    /// <summary>Extensible named bucket map (preferred when bound via OssSettings__Buckets__*).</summary>
    public Dictionary<string, OssBucketEndpoint> Buckets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Flat aliases (docker-compose / .env) → identities
    public string BucketName { get; set; } = "auth-and-identities-certificates-staging";
    public string PublicBaseUrl { get; set; } = "https://auth-and-identities-certificates-staging.oss-me-central-1.aliyuncs.com";

    // Flat aliases → learning (content library, open-session attachments, future course media)
    public string LearningBucketName { get; set; } = string.Empty;
    public string LearningPublicBaseUrl { get; set; } = string.Empty;

    public OssBucketEndpoint Resolve(string bucketKey)
    {
        if (Buckets.TryGetValue(bucketKey, out var fromMap)
            && !string.IsNullOrWhiteSpace(fromMap.BucketName))
        {
            return Normalize(fromMap);
        }

        if (string.Equals(bucketKey, OssBucketKeys.Identities, StringComparison.OrdinalIgnoreCase))
        {
            return new OssBucketEndpoint
            {
                BucketName = BucketName,
                PublicBaseUrl = PublicBaseUrl,
            };
        }

        if (string.Equals(bucketKey, OssBucketKeys.Learning, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(LearningBucketName))
            {
                return new OssBucketEndpoint
                {
                    BucketName = LearningBucketName,
                    PublicBaseUrl = string.IsNullOrWhiteSpace(LearningPublicBaseUrl)
                        ? PublicBaseUrl
                        : LearningPublicBaseUrl,
                };
            }

            // Legacy single-bucket deploys: learning falls back to identities
            return new OssBucketEndpoint
            {
                BucketName = BucketName,
                PublicBaseUrl = PublicBaseUrl,
            };
        }

        throw new InvalidOperationException(
            $"OSS bucket key '{bucketKey}' is not configured. Add Buckets['{bucketKey}'] or flat Learning* / BucketName settings.");
    }

    private static OssBucketEndpoint Normalize(OssBucketEndpoint endpoint) => new()
    {
        BucketName = endpoint.BucketName.Trim(),
        PublicBaseUrl = (endpoint.PublicBaseUrl ?? string.Empty).TrimEnd('/'),
    };
}
