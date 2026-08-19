using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IPricingSnapshotRepository : IGenericRepositoryAsync<PricingSnapshot>
{
    Task<PricingSnapshot?> GetByContextAsync(
        PricingSnapshotContext context,
        int contextEntityId,
        CancellationToken cancellationToken = default);
}
