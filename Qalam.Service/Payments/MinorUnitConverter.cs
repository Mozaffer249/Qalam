namespace Qalam.Service.Payments;

/// <summary>
/// Convert major currency units (SAR) to/from the smallest unit (halalas).
/// Shared by Moyasar, PayTabs, HyperPay, and Stripe (two-decimal SAR).
/// </summary>
public static class MinorUnitConverter
{
    public static int ToMinor(decimal amountMajor)
        => (int)Math.Round(amountMajor * 100m, MidpointRounding.AwayFromZero);

    public static decimal FromMinor(int amountMinor)
        => amountMinor / 100m;

    /// <summary>Alias kept for call sites that still say "halalas".</summary>
    public static int ToHalalas(decimal amountSar) => ToMinor(amountSar);

    public static decimal FromHalalas(int amountHalalas) => FromMinor(amountHalalas);
}
