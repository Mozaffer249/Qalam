using Qalam.Data.DTOs.Payment;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Payment service provider abstraction (Moyasar, PayTabs, HyperPay, Stripe, Mock).
/// Resolve by Payment.PaymentProvider for existing rows; use IPaymentGatewayResolver
/// for the active provider when creating new intents.
/// </summary>
public interface IPaymentGateway
{
    string ProviderName { get; }

    /// <summary>True when required env keys for this gateway are present.</summary>
    bool IsConfigured { get; }

    PaymentClientMode ClientMode { get; }

    Task<GatewayCheckoutDto> CreateCheckoutAsync(
        GatewayCheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<GatewayPaymentDto?> FetchAsync(string providerPaymentId, CancellationToken cancellationToken = default);

    Task<GatewayRefundDto> RefundAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default);

    Task<GatewayPaymentDto?> CaptureAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default);

    Task<GatewayPaymentDto?> VoidAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vendor-specific webhook auth + payload parse. Never mutates domain state.
    /// </summary>
    PaymentWebhookParseResult VerifyAndParseWebhook(
        string rawBody,
        IReadOnlyDictionary<string, string> headers);
}
