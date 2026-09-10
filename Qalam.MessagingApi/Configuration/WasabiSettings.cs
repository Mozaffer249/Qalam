namespace Qalam.MessagingApi.Configuration;

/// <summary>
/// Wasabi Hot Cloud Storage (S3-compatible). Same logical bucket keys as <see cref="OssSettings"/>.
/// </summary>
public class WasabiSettings
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string Region { get; set; } = "ap-southeast-1";
    /// <summary>S3 API endpoint for the bucket region (without bucket in path).</summary>
    public string Endpoint { get; set; } = "https://s3.ap-southeast-1.wasabisys.com";

    public Dictionary<string, OssBucketEndpoint> Buckets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string BucketName { get; set; } = "auth-and-identities-certificates-staging";
    public string PublicBaseUrl { get; set; } =
        "https://auth-and-identities-certificates-staging.s3.ap-southeast-1.wasabisys.com";

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

            return new OssBucketEndpoint
            {
                BucketName = BucketName,
                PublicBaseUrl = PublicBaseUrl,
            };
        }

        throw new InvalidOperationException(
            $"Wasabi bucket key '{bucketKey}' is not configured. Add Buckets['{bucketKey}'] or flat Learning* / BucketName settings.");
    }

    private static OssBucketEndpoint Normalize(OssBucketEndpoint endpoint) => new()
    {
        BucketName = endpoint.BucketName.Trim(),
        PublicBaseUrl = (endpoint.PublicBaseUrl ?? string.Empty).TrimEnd('/'),
    };
}
