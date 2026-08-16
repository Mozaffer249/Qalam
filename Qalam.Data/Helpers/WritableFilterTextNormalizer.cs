using System.Text.RegularExpressions;

namespace Qalam.Data.Helpers;

public static class WritableFilterTextNormalizer
{
    private static readonly Regex ExtraWhitespace = new(@"\s+", RegexOptions.Compiled);

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return ExtraWhitespace.Replace(text.Trim(), " ").ToLowerInvariant();
    }
}
