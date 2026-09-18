using Microsoft.Extensions.Configuration;

namespace Qalam.Service.Payments;

/// <summary>
/// Validates student-app payment return URLs against Cors:AllowedOrigins
/// so Moyasar cannot be pointed at an arbitrary origin.
/// </summary>
public static class PaymentAppReturnUrlHelper
{
    public static string[] ReadAllowedOrigins(IConfiguration configuration)
    {
        string[] allowedOrigins;
        var scalarCsv = configuration["Cors:AllowedOrigins"];
        if (!string.IsNullOrWhiteSpace(scalarCsv))
        {
            allowedOrigins = scalarCsv.Contains(',')
                ? scalarCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : [scalarCsv.Trim()];
        }
        else
        {
            allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? [];
        }

        return allowedOrigins
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim().TrimEnd('/'))
            .ToArray();
    }

    /// <summary>
    /// Returns a validated absolute URL, or null when the input is missing/invalid
    /// or its origin is not in <paramref name="allowedOrigins"/>.
    /// </summary>
    public static string? Sanitize(string? url, IReadOnlyList<string> allowedOrigins)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return null;

        var origin = $"{uri.Scheme}://{uri.Authority}";
        if (allowedOrigins.Count == 0
            || allowedOrigins.Any(o => o == "*"))
        {
            // Match CORS allow-any when no origins configured.
        }
        else
        {
            var allowed = allowedOrigins.Any(o =>
                string.Equals(o.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase));
            if (!allowed)
                return null;
        }

        // Drop fragment; keep path + query as provided by the client.
        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/')
            + (string.IsNullOrEmpty(uri.Query) ? string.Empty : uri.Query);
    }

    /// <summary>
    /// Prefer a validated client <paramref name="appQueryParam"/>; else the configured default.
    /// </summary>
    public static string? Resolve(
        string? appQueryParam,
        string? configuredAppReturnUrl,
        IReadOnlyList<string> allowedOrigins)
    {
        return Sanitize(appQueryParam, allowedOrigins)
            ?? Sanitize(configuredAppReturnUrl, allowedOrigins);
    }

    /// <summary>
    /// Appends <c>app=</c> to the API Moyasar return URL without breaking existing query params.
    /// </summary>
    public static string AppendAppQuery(string apiCallbackUrl, string appReturnUrl)
    {
        var separator = apiCallbackUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return apiCallbackUrl + separator + "app=" + Uri.EscapeDataString(appReturnUrl);
    }
}
