using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Payment;

public enum PaymentClientMode
{
    /// <summary>Native SDK on the client (e.g. Moyasar Flutter CreditCard).</summary>
    NativeSdk = 1,

    /// <summary>Open a hosted payment page in a WebView / browser.</summary>
    HostedRedirect = 2
}

public class GatewayCheckoutRequest
{
    public string GivenId { get; set; } = string.Empty;
    public int AmountHalalas { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// When set (e.g. Moyasar admin setting), overrides the gateway's env default ClientMode.
    /// </summary>
    public PaymentClientMode? PreferredClientMode { get; set; }
}

public class GatewayCheckoutDto
{
    public PaymentClientMode ClientMode { get; set; }
    public string ProviderPaymentRef { get; set; } = string.Empty;
    public string? PublishableKey { get; set; }
    public string? RedirectUrl { get; set; }
    public string? ClientSecret { get; set; }
    public string? CallbackUrl { get; set; }
    public string? ApplePayMerchantId { get; set; }
    public string? ApplePayLabel { get; set; }
}

public enum PaymentWebhookAuthResult
{
    Ok = 1,
    Unauthorized = 2,
    NotConfigured = 3,
    Ignored = 4
}

/// <summary>
/// Normalized webhook parse result produced by a gateway's VerifyAndParseWebhook.
/// </summary>
public class PaymentWebhookParseResult
{
    public PaymentWebhookAuthResult Auth { get; init; }
    public string? ProviderPaymentId { get; init; }
    public string? EventType { get; init; }
    public PaymentStatus? MappedStatus { get; init; }
    public string? Message { get; init; }

    public static PaymentWebhookParseResult Unauthorized() => new()
    {
        Auth = PaymentWebhookAuthResult.Unauthorized
    };

    public static PaymentWebhookParseResult NotConfigured() => new()
    {
        Auth = PaymentWebhookAuthResult.NotConfigured
    };

    public static PaymentWebhookParseResult Ignored(string? reason = null) => new()
    {
        Auth = PaymentWebhookAuthResult.Ignored,
        Message = reason
    };

    /// <summary>
    /// Alternate refs that may match a local Payment.ProviderTransactionId
    /// (e.g. Moyasar invoice id when the webhook carries the payment id).
    /// </summary>
    public IReadOnlyList<string>? AlternateProviderPaymentIds { get; init; }

    public static PaymentWebhookParseResult Success(
        string providerPaymentId,
        string? eventType,
        PaymentStatus? mappedStatus,
        string? message = null,
        IReadOnlyList<string>? alternateProviderPaymentIds = null) => new()
    {
        Auth = PaymentWebhookAuthResult.Ok,
        ProviderPaymentId = providerPaymentId,
        EventType = eventType,
        MappedStatus = mappedStatus,
        Message = message,
        AlternateProviderPaymentIds = alternateProviderPaymentIds
    };
}

/// <summary>Outcome of IPaymentWebhookService.HandleAsync for the thin controller.</summary>
public enum PaymentWebhookHandleStatus
{
    Ok = 1,
    Unauthorized = 2,
    NotConfigured = 3
}

public sealed class PaymentWebhookHandleResult
{
    public PaymentWebhookHandleStatus Status { get; init; }
    public string Message { get; init; } = "ok";

    public static PaymentWebhookHandleResult Ok(string message = "ok") => new()
    {
        Status = PaymentWebhookHandleStatus.Ok,
        Message = message
    };

    public static PaymentWebhookHandleResult Unauthorized(string message = "Invalid webhook signature.") => new()
    {
        Status = PaymentWebhookHandleStatus.Unauthorized,
        Message = message
    };

    public static PaymentWebhookHandleResult NotConfigured(string message = "Webhook not configured.") => new()
    {
        Status = PaymentWebhookHandleStatus.NotConfigured,
        Message = message
    };
}
