using Qalam.Data.DTOs.Payment;

namespace Qalam.Service.Abstracts;

public interface IPaymentConfirmationService
{
    /// <summary>
    /// Activates the enrollment linked to a succeeded (or about-to-succeed) payment:
    /// enrollment payments, schedules, OSR Paid. Idempotent when already Active.
    /// </summary>
    Task<PaymentConfirmationOutcome> ConfirmAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the payment from its recorded gateway, verifies status/amount/currency,
    /// updates Status/ProviderFee/FailureMessage, then runs ConfirmAsync.
    /// Shared by student Confirm and provider webhooks.
    /// </summary>
    Task<PaymentConfirmationOutcome> ConfirmFromGatewayAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentConfirmationOutcome
{
    public bool Succeeded { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public PaymentResultDto? Result { get; init; }

    public static PaymentConfirmationOutcome Ok(PaymentResultDto result) => new()
    {
        Succeeded = true,
        Result = result
    };

    public static PaymentConfirmationOutcome Fail(string code, string message) => new()
    {
        Succeeded = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
