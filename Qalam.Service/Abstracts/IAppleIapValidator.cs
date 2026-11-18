namespace Qalam.Service.Abstracts;

public interface IAppleIapValidator
{
    /// <summary>
    /// Validates an Apple IAP transaction by calling the App Store Server API v2.
    /// Returns the validated transaction details or an error.
    /// </summary>
    Task<AppleIapValidationResult> ValidateTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken = default);
}

public sealed class AppleIapValidationResult
{
    public bool Succeeded { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>Verified bundle ID from the signed transaction.</summary>
    public string? BundleId { get; init; }

    /// <summary>Verified product ID from the signed transaction.</summary>
    public string? ProductId { get; init; }

    /// <summary>Transaction environment (Production / Sandbox).</summary>
    public string? Environment { get; init; }

    /// <summary>Amount in milliunits (e.g. 50000 = 50.000 SAR).</summary>
    public long? PriceMilliunits { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string? Currency { get; init; }

    public static AppleIapValidationResult Ok(
        string bundleId, string productId, string environment,
        long priceMilliunits, string currency) => new()
    {
        Succeeded = true,
        BundleId = bundleId,
        ProductId = productId,
        Environment = environment,
        PriceMilliunits = priceMilliunits,
        Currency = currency,
    };

    public static AppleIapValidationResult Fail(string code, string message) => new()
    {
        Succeeded = false,
        ErrorCode = code,
        ErrorMessage = message,
    };
}
