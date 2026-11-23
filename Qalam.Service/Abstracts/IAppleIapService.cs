namespace Qalam.Service.Abstracts;

public interface IAppleIapService
{
    /// <summary>
    /// Returns the App Store product ID that covers <paramref name="amountSar"/>,
    /// or null when the amount exceeds all configured tiers.
    /// </summary>
    string? ResolveProductId(decimal amountSar);

    /// <summary>
    /// Creates a signed JWT token that encodes the IAP intent (participantId, userId,
    /// productId, amountSar). Pass this as <c>applicationUserName</c> to StoreKit.
    /// </summary>
    string CreateTransactionToken(int participantId, int userId, string productId, decimal amountSar);

    /// <summary>
    /// Validates and decodes a <see cref="CreateTransactionToken"/> token.
    /// Returns false if the token is expired, tampered, or missing required claims.
    /// </summary>
    bool TryParseTransactionToken(
        string token,
        out int participantId,
        out int userId,
        out string productId,
        out decimal amountSar);

    /// <summary>
    /// Calls the App Store Server API to verify a completed transaction.
    /// Returns null when <c>AppleIapSettings.Enabled</c> is false (falls through to mock confirm).
    /// </summary>
    Task<AppleTransactionInfo?> VerifyTransactionAsync(string transactionId, CancellationToken ct = default);
}

/// <summary>Minimal projection of an App Store Server API transaction.</summary>
public sealed class AppleTransactionInfo
{
    public string BundleId { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    /// <summary>The <c>applicationUsername</c> set by the client (our transactionToken).</summary>
    public string ApplicationUsername { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public long PurchaseDateMs { get; init; }
    public string TransactionId { get; init; } = string.Empty;
}
