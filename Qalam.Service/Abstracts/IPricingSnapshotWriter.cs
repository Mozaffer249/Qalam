using Qalam.Data.Entity.Pricing;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Abstracts;

public interface IPricingSnapshotWriter
{
    Task<PricingSnapshot> CreateAndSaveAsync(
        CreatePricingSnapshotRequest request,
        CancellationToken cancellationToken = default);
}
