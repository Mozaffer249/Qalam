using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;
using Qalam.Service.Payments;
using Microsoft.Extensions.Options;

namespace Qalam.Service.Implementations;

/// <summary>
/// Dev/test gateway that treats any known id as already paid.
/// </summary>
public class MockPaymentGateway : IPaymentGateway
{
    public const string Name = "Mock";

    private readonly PaymentSettings _settings;

    public MockPaymentGateway()
        : this(Microsoft.Extensions.Options.Options.Create(new PaymentSettings()))
    {
    }

    public MockPaymentGateway(IOptions<PaymentSettings> settings)
    {
        _settings = settings.Value;
    }

    public string ProviderName => Name;

    public bool IsConfigured => true;

    public PaymentClientMode ClientMode => PaymentClientMode.NativeSdk;

    public Task<GatewayCheckoutDto> CreateCheckoutAsync(
        GatewayCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GatewayCheckoutDto
        {
            ClientMode = ClientMode,
            ProviderPaymentRef = request.GivenId,
            PublishableKey = "mock_pk",
            CallbackUrl = request.CallbackUrl
        });
    }

    public Task<GatewayPaymentDto?> FetchAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return Task.FromResult<GatewayPaymentDto?>(null);

        return Task.FromResult<GatewayPaymentDto?>(new GatewayPaymentDto
        {
            Id = providerPaymentId,
            Status = "paid",
            MappedStatus = PaymentStatus.Succeeded,
            AmountHalalas = 0,
            Currency = _settings.DefaultCurrency,
            FeeHalalas = 0
        });
    }

    public Task<GatewayRefundDto> RefundAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GatewayRefundDto
        {
            Id = ("MOCK-REF-" + Guid.NewGuid().ToString("N"))[..24].ToUpperInvariant(),
            Status = "refunded",
            AmountHalalas = amountHalalas ?? 0,
            Currency = _settings.DefaultCurrency
        });
    }

    public Task<GatewayPaymentDto?> CaptureAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default)
        => FetchAsync(providerPaymentId, cancellationToken);

    public Task<GatewayPaymentDto?> VoidAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<GatewayPaymentDto?>(new GatewayPaymentDto
        {
            Id = providerPaymentId,
            Status = "voided",
            MappedStatus = PaymentStatus.Failed,
            AmountHalalas = 0,
            Currency = _settings.DefaultCurrency
        });
    }

    public PaymentWebhookParseResult VerifyAndParseWebhook(
        string rawBody,
        IReadOnlyDictionary<string, string> headers)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return PaymentWebhookParseResult.Ignored("empty");

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(id))
                return PaymentWebhookParseResult.Ignored("no id");

            return PaymentWebhookParseResult.Success(id, "payment_paid", PaymentStatus.Succeeded);
        }
        catch
        {
            return PaymentWebhookParseResult.Ignored("invalid json");
        }
    }
}
