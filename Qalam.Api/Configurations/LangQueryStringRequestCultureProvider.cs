using Microsoft.AspNetCore.Localization;

namespace Qalam.Api.Configurations;

/// <summary>
/// Maps query <c>?lang=ar|en</c> (and common variants) onto the app's supported cultures.
/// </summary>
public sealed class LangQueryStringRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var lang = httpContext.Request.Query["lang"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(lang))
            return NullProviderCultureResult;

        var normalized = lang.Trim().ToLowerInvariant();
        var culture = normalized switch
        {
            "ar" or "ar-eg" or "ar-sa" => "ar-EG",
            "en" or "en-us" or "en-gb" => "en-US",
            _ when normalized.StartsWith("ar", StringComparison.Ordinal) => "ar-EG",
            _ when normalized.StartsWith("en", StringComparison.Ordinal) => "en-US",
            _ => null
        };

        if (culture is null)
            return NullProviderCultureResult;

        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
    }
}
