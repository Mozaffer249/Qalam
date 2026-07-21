using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class MediaUrlResolver : IMediaUrlResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PlatformSettings _platformSettings;

    public MediaUrlResolver(
        IHttpContextAccessor httpContextAccessor,
        IOptions<PlatformSettings> platformSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _platformSettings = platformSettings.Value;
    }

    public string? ToPublicUrl(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return null;

        var path = storedPath.Replace('\\', '/').Trim();

        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var baseUrl = ResolveApiPublicBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            // No request / config — return rooted relative path so clients can join their API base.
            return "/" + path.TrimStart('/');
        }

        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private string? ResolveApiPublicBaseUrl()
    {
        var configured = _platformSettings.ApiPublicBaseUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
            return null;

        return $"{request.Scheme}://{request.Host.Value}".TrimEnd('/');
    }
}
