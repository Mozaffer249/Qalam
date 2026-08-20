using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IPricingMarketRepository : IGenericRepositoryAsync<PricingMarket>
{
    Task<List<PricingMarket>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<PricingMarket?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<PricingMarket?> GetDefaultAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveAsync(string code, CancellationToken cancellationToken = default);
}
