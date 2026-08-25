using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Pricing;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Abstracts;

public interface IPricingSnapshotWriter
{
    Task<PricingSnapshot> CreateAndSaveAsync(
        CreatePricingSnapshotRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies an existing frozen quote into a new context (e.g. OSR → offer) without re-estimating.
    /// </summary>
    Task<PricingSnapshot> CloneForContextAsync(
        PricingSnapshot source,
        PricingSnapshotContext context,
        int contextEntityId,
        CancellationToken cancellationToken = default);
}
