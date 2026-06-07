namespace Qalam.MessagingApi.Configuration;

public class OssSettings
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string BucketName { get; set; } = "auth-and-identities-certificates-staging";
    public string Region { get; set; } = "me-central-1";
    /// <summary>API endpoint for uploads (public or internal, without bucket in path).</summary>
    public string Endpoint { get; set; } = "https://oss-me-central-1.aliyuncs.com";
    /// <summary>HTTPS base used in DB URLs (virtual-hosted bucket domain).</summary>
    public string PublicBaseUrl { get; set; } = "https://auth-and-identities-certificates-staging.oss-me-central-1.aliyuncs.com";
    public bool UseInternalEndpoint { get; set; }
    /// <summary>
    /// When set AND AccessKeyId/Secret are blank, the SDK fetches short-lived STS tokens from
    /// the ECS instance metadata service for this role. Used on Aliyun ECS (staging/prod) to
    /// avoid long-lived AccessKeys in env files. Leave empty for local dev (laptop, not on ECS).
    /// </summary>
    public string? EcsRoleName { get; set; }
}
