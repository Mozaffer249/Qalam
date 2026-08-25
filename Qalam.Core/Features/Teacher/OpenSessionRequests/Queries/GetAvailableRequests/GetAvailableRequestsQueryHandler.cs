using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequests;

public class GetAvailableRequestsQueryHandler : ResponseHandler,
    IRequestHandler<GetAvailableRequestsQuery, Response<PaginatedResult<TeacherAvailableRequestListItemDto>>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;
    private readonly ApplicationDBContext _db;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingMarketResolver _marketResolver;
    private readonly ITargetedOpenSessionRequestPricingService _targetedPricing;

    public GetAvailableRequestsQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestTargetRepository targetRepo,
        ApplicationDBContext db,
        IPricingEngine pricingEngine,
        IPricingMarketResolver marketResolver,
        ITargetedOpenSessionRequestPricingService targetedPricing) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _targetRepo = targetRepo;
        _db = db;
        _pricingEngine = pricingEngine;
        _marketResolver = marketResolver;
        _targetedPricing = targetedPricing;
    }

    public async Task<Response<PaginatedResult<TeacherAvailableRequestListItemDto>>> Handle(
        GetAvailableRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<PaginatedResult<TeacherAvailableRequestListItemDto>>("Teacher account not active.");

        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var pageNumber = Math.Max(1, request.PageNumber);

        var filters = new TeacherInboxFilters(
            request.Status,
            request.SubjectId,
            request.DateFrom,
            request.DateTo,
            pageNumber,
            pageSize,
            request.SortBy,
            request.IsTargeted,
            request.RequestStatus,
            request.Scope);

        var page = await _targetRepo.GetTeacherInboxAsync(teacher.Id, filters, cancellationToken);
        await EnrichPricingAsync(page.Items, teacher.Id, request.UserId, cancellationToken);
        return Success(entity: page);
    }

    private async Task EnrichPricingAsync(
        List<TeacherAvailableRequestListItemDto> items,
        int teacherId,
        int userId,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var snapshotIds = items
            .Where(i => i.PricingSnapshotId.HasValue)
            .Select(i => i.PricingSnapshotId!.Value)
            .Distinct()
            .ToList();

        var snapshots = snapshotIds.Count == 0
            ? new Dictionary<int, Data.Entity.Pricing.PricingSnapshot>()
            : await _db.PricingSnapshots.AsNoTracking()
                .Where(s => snapshotIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);

        string? marketCode = null;
        foreach (var item in items)
        {
            if (item.TotalMinutes <= 0)
                continue;

            try
            {
                if (item.PricingSnapshotId.HasValue
                    && snapshots.TryGetValue(item.PricingSnapshotId.Value, out var frozen)
                    && frozen.TeacherId == teacherId)
                {
                    var dto = _targetedPricing.ToEstimateDto(frozen);
                    item.TotalPrice = dto.TotalPrice;
                    item.Currency = dto.Currency;
                    item.TeacherEarnings = dto.TeacherEarnings;
                    continue;
                }

                marketCode ??= (await _marketResolver.ResolveForUserAsync(userId, cancellationToken)).MarketCode;
                var sessionTypeCode = item.GroupType.HasValue ? "group" : "individual";
                var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
                {
                    DomainId = item.DomainId,
                    SessionTypeCode = sessionTypeCode,
                    MarketCode = marketCode,
                    TotalMinutes = item.TotalMinutes,
                    TeacherId = teacherId
                }, cancellationToken);

                item.TotalPrice = estimate.TotalPrice;
                item.Currency = estimate.Currency;
                item.TeacherEarnings = estimate.TeacherEarnings;
            }
            catch (InvalidOperationException)
            {
                // No configured rate — leave price null.
            }
        }
    }
}
