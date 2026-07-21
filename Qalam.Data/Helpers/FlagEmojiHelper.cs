namespace Qalam.Data.Helpers;

/// <summary>
/// Builds Unicode regional-indicator flag emoji from an ISO 3166-1 alpha-2 code (e.g. SA → 🇸🇦).
/// </summary>
public static class FlagEmojiHelper
{
    public static string FromIso2(string? iso2)
    {
        if (string.IsNullOrWhiteSpace(iso2) || iso2.Trim().Length != 2)
            return string.Empty;

        var code = iso2.Trim().ToUpperInvariant();
        if (code[0] is < 'A' or > 'Z' || code[1] is < 'A' or > 'Z')
            return string.Empty;

        return string.Concat(
            char.ConvertFromUtf32(0x1F1E6 + (code[0] - 'A')),
            char.ConvertFromUtf32(0x1F1E6 + (code[1] - 'A')));
    }

    public static string Resolve(string? stored, string? iso2)
    {
        if (!string.IsNullOrWhiteSpace(stored))
            return stored.Trim();
        return FromIso2(iso2);
    }
}
