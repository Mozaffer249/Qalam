using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _byName;
    private readonly IPaymentGatewaySettingsProvider _settingsProvider;

    public PaymentGatewayResolver(
        IEnumerable<IPaymentGateway> gateways,
        IPaymentGatewaySettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
        var map = new Dictionary<string, IPaymentGateway>(StringComparer.OrdinalIgnoreCase);
        foreach (var gateway in gateways)
        {
            map[gateway.ProviderName] = gateway;
            // Mock is also stored as MOCK in older rows via MockProviderName.
            if (gateway.ProviderName.Equals(MockPaymentGateway.Name, StringComparison.OrdinalIgnoreCase))
                map["MOCK"] = gateway;
        }

        _byName = map;
        All = map.Values.Distinct().ToList();
    }

    public IReadOnlyList<IPaymentGateway> All { get; }

    public IPaymentGateway Resolve(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new InvalidOperationException("Payment provider name is required.");

        if (_byName.TryGetValue(providerName.Trim(), out var gateway))
            return gateway;

        throw new InvalidOperationException(
            $"Unsupported payment provider '{providerName}'. Implement IPaymentGateway and register it.");
    }

    public async Task<IPaymentGateway> ResolveActiveAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsProvider.GetSettingsAsync(cancellationToken);
        var gateway = Resolve(settings.ActiveProvider);
        if (!gateway.IsConfigured
            && !gateway.ProviderName.Equals(MockPaymentGateway.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Payment provider '{gateway.ProviderName}' is selected but not configured (missing env keys).");
        }

        return gateway;
    }
}
