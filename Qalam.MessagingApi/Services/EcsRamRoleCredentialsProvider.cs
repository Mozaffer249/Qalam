using System.Text.Json;
using Aliyun.OSS.Common.Authentication;

namespace Qalam.MessagingApi.Services;

/// <summary>
/// Aliyun OSS credentials provider that fetches short-lived STS tokens from the ECS instance
/// metadata service (IMDSv1) for the bound RAM role and caches them until shortly before expiry.
///
/// Endpoint: <c>http://100.100.100.200/latest/meta-data/ram/security-credentials/{roleName}</c>
/// Returns JSON with <c>AccessKeyId</c> / <c>AccessKeySecret</c> / <c>SecurityToken</c> / <c>Expiration</c>.
/// Aliyun rotates the token every ~6 hours; we refresh 5 minutes before <c>Expiration</c>.
///
/// Only valid when the app is running on an Aliyun ECS instance that has a RAM role bound. On a
/// laptop or non-Aliyun host this will fail with a connection error — use static AccessKeys there.
/// </summary>
public class EcsRamRoleCredentialsProvider : ICredentialsProvider
{
    private const string MetadataBaseUrl = "http://100.100.100.200/latest/meta-data/ram/security-credentials/";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    private readonly string _roleName;
    private readonly ILogger? _logger;
    private readonly object _lock = new();

    private ICredentials? _cached;
    private DateTime _expiryUtc = DateTime.MinValue;

    public EcsRamRoleCredentialsProvider(string roleName, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name is required", nameof(roleName));
        _roleName = roleName;
        _logger = logger;
    }

    public ICredentials GetCredentials()
    {
        lock (_lock)
        {
            if (_cached == null || DateTime.UtcNow >= _expiryUtc - RefreshSkew)
                Refresh();
            return _cached!;
        }
    }

    public void SetCredentials(ICredentials creds)
    {
        lock (_lock)
        {
            _cached = creds;
            _expiryUtc = DateTime.UtcNow.AddHours(1);
        }
    }

    private void Refresh()
    {
        var url = MetadataBaseUrl + Uri.EscapeDataString(_roleName);
        string body;
        try
        {
            body = HttpClient.GetStringAsync(url).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to fetch STS token from ECS metadata for role '{Role}'. " +
                "Are you running on Aliyun ECS with this RAM role bound?", _roleName);
            throw new InvalidOperationException(
                $"Failed to fetch STS token from ECS metadata for role '{_roleName}': {ex.Message}", ex);
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("Code", out var code) && code.GetString() != "Success")
            throw new InvalidOperationException($"ECS metadata returned non-Success Code: {code.GetString()}");

        var accessKeyId = root.GetProperty("AccessKeyId").GetString()
            ?? throw new InvalidOperationException("ECS metadata missing AccessKeyId");
        var accessKeySecret = root.GetProperty("AccessKeySecret").GetString()
            ?? throw new InvalidOperationException("ECS metadata missing AccessKeySecret");
        var securityToken = root.GetProperty("SecurityToken").GetString()
            ?? throw new InvalidOperationException("ECS metadata missing SecurityToken");
        var expiration = root.GetProperty("Expiration").GetString()
            ?? throw new InvalidOperationException("ECS metadata missing Expiration");

        _cached = new DefaultCredentials(accessKeyId, accessKeySecret, securityToken);
        _expiryUtc = DateTime.Parse(expiration, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

        _logger?.LogInformation(
            "Refreshed ECS STS token for role '{Role}'; expires at {ExpiryUtc:o}",
            _roleName, _expiryUtc);
    }
}
