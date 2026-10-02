using Qalam.Data.DTOs.Payment;

namespace Qalam.Service.Abstracts;

public interface IPaymentIntentService
{
    /// <summary>
    /// Creates a Pending Payment for the participant against the active gateway
    /// and returns the client checkout payload.
    /// </summary>
    Task<PaymentIntentServiceResult> CreateAsync(
        int participantId,
        int userId,
        string? appReturnUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Pending wallet top-up payment against the active gateway. Confirmation
    /// (client confirm, webhook, reconciliation) credits the wallet.
    /// </summary>
    Task<PaymentIntentServiceResult> CreateWalletTopUpAsync(
        int walletId,
        int userId,
        decimal amount,
        string? appReturnUrl = null,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentIntentServiceResult
{
    public bool Succeeded { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public PaymentIntentDto? Intent { get; init; }

    public static PaymentIntentServiceResult Ok(PaymentIntentDto intent) => new()
    {
        Succeeded = true,
        Intent = intent
    };

    public static PaymentIntentServiceResult Fail(string code, string message) => new()
    {
        Succeeded = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
