namespace Qalam.Data.Helpers;

public class PlatformSettings
{
    /// <summary>
    /// Base URL for web-app login CTAs in emails (teacher now; student/other emails later).
    /// </summary>
    public string WebAppBaseUrl { get; set; } = "https://qalam.net.sa/";

    /// <summary>
    /// Public origin of the main API (no trailing slash), used to absolute-ize relative upload paths
    /// like <c>uploads/courses/…</c> in API responses. When empty, the current request's
    /// scheme + host is used (fine for local Docker; set explicitly behind reverse proxies).
    /// Example: <c>https://api.qalam.net.sa</c> or <c>http://localhost:8080</c>.
    /// </summary>
    public string? ApiPublicBaseUrl { get; set; }
}
