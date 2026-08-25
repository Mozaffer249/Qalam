using Qalam.Data.Entity.Common.Enums;
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

    public async Task<PricingSnapshot> CloneForContextAsync(
        PricingSnapshot source,
        PricingSnapshotContext context,
        int contextEntityId,
        CancellationToken cancellationToken = default)
    {
        var clone = new PricingSnapshot
        {
            Context = context,
            ContextEntityId = contextEntityId,
            DomainId = source.DomainId,
            SessionTypeCode = source.SessionTypeCode,
            MarketCode = source.MarketCode,
            Currency = source.Currency,
            DomainSessionPriceId = source.DomainSessionPriceId,
            PricePerHour = source.PricePerHour,
            TotalMinutes = source.TotalMinutes,
            TotalPrice = source.TotalPrice,
            TeacherId = source.TeacherId,
            TeacherLevelId = source.TeacherLevelId,
            TeacherSharePct = source.TeacherSharePct,
            TeacherEarnings = source.TeacherEarnings,
            PlatformShare = source.PlatformShare,
            ReflectCustomPriceToStudent = source.ReflectCustomPriceToStudent,
            EarningsPricePerHour = source.EarningsPricePerHour,
            CreatedAt = DateTime.UtcNow
        };

        await _snapshotRepository.AddAsync(clone);
        await _snapshotRepository.SaveChangesAsync();
        return clone;
    }
}
