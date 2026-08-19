using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Implementations;

public class PricingSnapshotWriter : IPricingSnapshotWriter
{
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingSnapshotRepository _snapshotRepository;

    public PricingSnapshotWriter(
        IPricingEngine pricingEngine,
        IPricingSnapshotRepository snapshotRepository)
    {
        _pricingEngine = pricingEngine;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<PricingSnapshot> CreateAndSaveAsync(
        CreatePricingSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _pricingEngine.CreateSnapshotAsync(request, cancellationToken);
        await _snapshotRepository.AddAsync(snapshot);
        await _snapshotRepository.SaveChangesAsync();
        return snapshot;
    }
}
