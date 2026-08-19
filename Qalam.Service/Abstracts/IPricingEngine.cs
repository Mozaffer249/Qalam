using Qalam.Data.Entity.Pricing;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Abstracts;

public interface IPricingEngine
{
    Task<PriceEstimate> EstimateAsync(PricingEstimateRequest request, CancellationToken cancellationToken = default);

    Task<PricingSnapshot> CreateSnapshotAsync(
        CreatePricingSnapshotRequest request,
        CancellationToken cancellationToken = default);

    Task<decimal> ResolvePricePerHourAsync(
        int domainId,
        string sessionTypeCode,
        DateTime? asOf = null,
        CancellationToken cancellationToken = default);
}
