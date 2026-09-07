using Qalam.Data.DTOs.Payment;

namespace Qalam.Service.Abstracts;

public interface IPaymentWebhookService
{
    Task<PaymentWebhookHandleResult> HandleAsync(
        string provider,
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default);
}
