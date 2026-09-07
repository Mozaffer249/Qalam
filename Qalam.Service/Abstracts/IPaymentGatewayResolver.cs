namespace Qalam.Service.Abstracts;

/// <summary>
/// Registry of IPaymentGateway implementations.
/// Resolve by stored Payment.PaymentProvider for confirm/refund/webhook;
/// ResolveActiveAsync for new intents.
/// </summary>
public interface IPaymentGatewayResolver
{
    IPaymentGateway Resolve(string providerName);

    Task<IPaymentGateway> ResolveActiveAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<IPaymentGateway> All { get; }
}
