using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Implementations;

public class TargetedOpenSessionRequestPricingService : ITargetedOpenSessionRequestPricingService
{
    private readonly IPricingMarketResolver _marketResolver;
    private readonly IPricingSnapshotWriter _pricingSnapshotWriter;
    private readonly IOpenSessionRequestRepository _requestRepository;

    public TargetedOpenSessionRequestPricingService(
        IPricingMarketResolver marketResolver,
        IPricingSnapshotWriter pricingSnapshotWriter,
        IOpenSessionRequestRepository requestRepository)
    {
        _marketResolver = marketResolver;
        _pricingSnapshotWriter = pricingSnapshotWriter;
        _requestRepository = requestRepository;
    }

    public async Task FreezeIfNeededAsync(
        OpenSessionRequest request,
        int marketUserId,
        CancellationToken cancellationToken = default)
    {
        if (!request.TargetedTeacherId.HasValue || request.PricingSnapshotId.HasValue)
            return;

        var totalMinutes = request.Sessions?.Sum(s => s.DurationMinutes) ?? 0;
        if (totalMinutes <= 0)
            return;

        var sessionTypeCode = request.GroupType.HasValue ? "group" : "individual";
        var market = await _marketResolver.ResolveForUserAsync(marketUserId, cancellationToken);
        var snapshot = await _pricingSnapshotWriter.CreateAndSaveAsync(new CreatePricingSnapshotRequest
        {
            Context = PricingSnapshotContext.OpenSessionRequest,
            ContextEntityId = request.Id,
            DomainId = request.DomainId,
            SessionTypeCode = sessionTypeCode,
            MarketCode = market.MarketCode,
            TotalMinutes = totalMinutes,
            TeacherId = request.TargetedTeacherId.Value
        }, cancellationToken);

        request.PricingSnapshotId = snapshot.Id;
        await _requestRepository.UpdateAsync(request);
        await _requestRepository.SaveChangesAsync();
    }

    public PricingEstimateDto ToEstimateDto(PricingSnapshot snapshot) =>
        new()
        {
            PricePerHour = snapshot.PricePerHour,
            Currency = snapshot.Currency,
            MarketCode = snapshot.MarketCode,
            TotalMinutes = snapshot.TotalMinutes,
            TotalPrice = snapshot.TotalPrice,
            TeacherSharePct = snapshot.TeacherSharePct,
            TeacherEarnings = snapshot.TeacherEarnings,
            PlatformShare = snapshot.PlatformShare,
            EarningsPricePerHour = snapshot.EarningsPricePerHour,
            ReflectCustomPriceToStudent = snapshot.ReflectCustomPriceToStudent
        };
}
