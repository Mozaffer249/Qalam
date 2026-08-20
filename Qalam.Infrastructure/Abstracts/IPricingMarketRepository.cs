using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IPricingMarketRepository : IGenericRepositoryAsync<PricingMarket>
{
    Task<List<PricingMarket>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<List<PricingMarket>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<PricingMarket?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<PricingMarket?> GetByCodeTrackedAsync(string code, CancellationToken cancellationToken = default);

    Task<PricingMarket?> GetDefaultAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveAsync(string code, CancellationToken cancellationToken = default);

    Task ClearDefaultFlagAsync(CancellationToken cancellationToken = default);

    Task<bool> HasUserPreferencesAsync(string code, CancellationToken cancellationToken = default);
}
