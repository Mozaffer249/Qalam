namespace Qalam.Data.AppMetaData;

public static class PricingExchangeRateHelper
{
    public static decimal DeriveLocalPrice(decimal basePrice, decimal exchangeRateFromBase) =>
        Math.Round(basePrice * exchangeRateFromBase, 2, MidpointRounding.AwayFromZero);
}
