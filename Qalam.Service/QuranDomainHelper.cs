namespace Qalam.Service;

/// <summary>
/// Shared detection for Quran education domain (code <c>quran</c> or name containing "quran").
/// </summary>
public static class QuranDomainHelper
{
    public const string DomainCode = "quran";

    public static bool IsQuranDomain(string? code, string? nameEn = null)
    {
        if (!string.IsNullOrWhiteSpace(code)
            && code.Equals(DomainCode, StringComparison.OrdinalIgnoreCase))
            return true;

        return !string.IsNullOrWhiteSpace(nameEn)
            && nameEn.Contains(DomainCode, StringComparison.OrdinalIgnoreCase);
    }
}
