using Qalam.Data.Entity.Pricing;

namespace Qalam.Data.AppMetaData;

public static class PricingMarketDefaults
{
    public const string DefaultMarketCode = "sa";

    public static IReadOnlyList<PricingMarketSeed> CreateMarkets(DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        return
        [
            new("sa", "SAR", "Saudi Arabia", "المملكة العربية السعودية", true, now),
            new("ae", "AED", "United Arab Emirates", "الإمارات العربية المتحدة", false, now),
            new("kw", "KWD", "Kuwait", "الكويت", false, now),
            new("qa", "QAR", "Qatar", "قطر", false, now),
            new("bh", "BHD", "Bahrain", "البحرين", false, now),
            new("om", "OMR", "Oman", "عُمان", false, now),
            new("eg", "EGP", "Egypt", "مصر", false, now),
            new("jo", "JOD", "Jordan", "الأردن", false, now),
        ];
    }

    /// <summary>ISO 3166-1 alpha-2 country code → market code.</summary>
    public static IReadOnlyDictionary<string, string> CountryToMarket { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SA"] = "sa",
            ["YE"] = "sa",
            ["SD"] = "sa",
            ["AE"] = "ae",
            ["KW"] = "kw",
            ["QA"] = "qa",
            ["BH"] = "bh",
            ["OM"] = "om",
            ["EG"] = "eg",
            ["JO"] = "jo",
        };

    /// <summary>Phone dial code (without +) → market code.</summary>
    public static IReadOnlyDictionary<string, string> DialCodeToMarket { get; } =
        new Dictionary<string, string>
        {
            ["966"] = "sa",
            ["971"] = "ae",
            ["965"] = "kw",
            ["974"] = "qa",
            ["973"] = "bh",
            ["968"] = "om",
            ["20"] = "eg",
            ["962"] = "jo",
        };

    public static string? ResolveMarketFromCountry(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return null;
        return CountryToMarket.TryGetValue(countryCode.Trim(), out var market) ? market : null;
    }

    public static string? ResolveMarketFromPhone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
            digits = digits[2..];

        foreach (var (dialCode, market) in DialCodeToMarket.OrderByDescending(x => x.Key.Length))
        {
            if (digits.StartsWith(dialCode, StringComparison.Ordinal))
                return market;
        }

        return null;
    }

    public sealed record PricingMarketSeed(
        string Code,
        string Currency,
        string NameEn,
        string NameAr,
        bool IsDefault,
        DateTime CreatedAt);
}
