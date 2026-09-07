namespace Qalam.Service.Payments;

/// <summary>
/// Backward-compatible alias for <see cref="MinorUnitConverter"/>.
/// Prefer MinorUnitConverter for new code.
/// </summary>
public static class MoyasarAmountConverter
{
    public static int ToHalalas(decimal amountSar) => MinorUnitConverter.ToHalalas(amountSar);

    public static decimal FromHalalas(int amountHalalas) => MinorUnitConverter.FromHalalas(amountHalalas);
}
