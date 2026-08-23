using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestById;

public class GetAvailableRequestByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetAvailableRequestByIdQuery, Response<TeacherAvailableRequestDetailDto>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;
    private readonly IOpenSessionOfferRepository _offerRepo;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingMarketResolver _marketResolver;

    public GetAvailableRequestByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestRepository requestRepo,
        IOpenSessionRequestTargetRepository targetRepo,
        IOpenSessionOfferRepository offerRepo,
        IPricingEngine pricingEngine,
        IPricingMarketResolver marketResolver) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _requestRepo = requestRepo;
        _targetRepo = targetRepo;
        _offerRepo = offerRepo;
        _pricingEngine = pricingEngine;
        _marketResolver = marketResolver;
    }

    public async Task<Response<TeacherAvailableRequestDetailDto>> Handle(
        GetAvailableRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<TeacherAvailableRequestDetailDto>("Teacher account not active.");

        // Authorization: the teacher must be targeted on this request.
        var target = await _targetRepo.GetByRequestAndTeacherAsync(request.RequestId, teacher.Id, cancellationToken);
        if (target == null)
            return Forbidden<TeacherAvailableRequestDetailDto>("NOT_MATCHED");

        var detail = await _requestRepo.GetTeacherDetailDtoAsync(request.RequestId, cancellationToken);
        if (detail == null)
            return NotFound<TeacherAvailableRequestDetailDto>("Request not found.");

        detail.TargetStatus = target.Status;

        // Side effect: flip the target row to Viewed on first detail open.
        if (target.Status == OpenSessionRequestTargetStatus.Notified)
        {
            await _targetRepo.SetStatusAsync(request.RequestId, teacher.Id, OpenSessionRequestTargetStatus.Viewed, cancellationToken);
            detail.TargetStatus = OpenSessionRequestTargetStatus.Viewed;
        }

        // Hydrate the caller's own offer state for the UI (button labels + sticky card).
        var existing = await _offerRepo.GetExistingActiveOfferAsync(request.RequestId, teacher.Id, cancellationToken);
        if (existing != null)
        {
            detail.MyOfferId = existing.Value.OfferId;
            detail.MyOfferStatus = existing.Value.Status;
        }

        if (existing == null)
        {
            var totalMinutes = detail.Sessions.Sum(s => s.DurationMinutes);
            if (totalMinutes > 0)
            {
                var sessionTypeCode = detail.GeneralSettings.GroupType.HasValue ? "group" : "individual";
                try
                {
                    var market = await _marketResolver.ResolveForUserAsync(request.UserId, cancellationToken);
                    var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
                    {
                        DomainId = detail.Content.DomainId,
                        SessionTypeCode = sessionTypeCode,
                        MarketCode = market.MarketCode,
                        TotalMinutes = totalMinutes,
                        TeacherId = teacher.Id
                    }, cancellationToken);

                    detail.PricingEstimate = new PricingEstimateDto
                    {
                        PricePerHour = estimate.PricePerHour,
                        Currency = estimate.Currency,
                        MarketCode = estimate.MarketCode,
                        TotalMinutes = estimate.TotalMinutes,
                        TotalPrice = estimate.TotalPrice,
                        TeacherSharePct = estimate.TeacherSharePct,
                        TeacherEarnings = estimate.TeacherEarnings,
                        PlatformShare = estimate.PlatformShare,
                        EarningsPricePerHour = estimate.EarningsPricePerHour,
                        ReflectCustomPriceToStudent = estimate.ReflectCustomPriceToStudent
                    };
                }
                catch (InvalidOperationException)
                {
                    // No configured rate — UI falls back without a preview.
                }
            }
        }

        return Success(entity: detail);
    }
}
