using Microsoft.AspNetCore.Http;

namespace Qalam.Core.Helpers;

public static class ClientIpHelper
{
    public static string GetClientIpAddress(HttpContext? context)
    {
        if (context == null)
            return "unknown";

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        var realIp = context.Request.Headers["X-Real-IP"].ToString();
        if (!string.IsNullOrWhiteSpace(realIp))
            return realIp.Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static string? GetUserAgent(HttpContext? context) =>
        context?.Request.Headers.UserAgent.ToString();
}
