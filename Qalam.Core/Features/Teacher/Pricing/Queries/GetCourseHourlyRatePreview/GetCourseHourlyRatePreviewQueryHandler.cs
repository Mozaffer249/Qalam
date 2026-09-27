using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Qalam.Service.Models.Pricing;

namespace Qalam.Core.Features.Teacher.Pricing.Queries.GetCourseHourlyRatePreview;

public class GetCourseHourlyRatePreviewQueryHandler : ResponseHandler,
    IRequestHandler<GetCourseHourlyRatePreviewQuery, Response<CourseHourlyRatePreviewDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ISessionTypeRepository _sessionTypeRepository;
    private readonly ITeacherDomainPricingRepository _domainPricingRepository;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingMarketResolver _marketResolver;

    public GetCourseHourlyRatePreviewQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        ISessionTypeRepository sessionTypeRepository,
        ITeacherDomainPricingRepository domainPricingRepository,
        IPricingEngine pricingEngine,
        IPricingMarketResolver marketResolver) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _sessionTypeRepository = sessionTypeRepository;
        _domainPricingRepository = domainPricingRepository;
        _pricingEngine = pricingEngine;
        _marketResolver = marketResolver;
    }

    public async Task<Response<CourseHourlyRatePreviewDto>> Handle(
        GetCourseHourlyRatePreviewQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<CourseHourlyRatePreviewDto>("Teacher not found");

        var teacherSubject = await _teacherSubjectRepository.GetByIdForTeacherAsync(
            teacher.Id,
            request.TeacherSubjectId,
            cancellationToken);
        if (teacherSubject == null)
            return NotFound<CourseHourlyRatePreviewDto>("Teacher subject not found");

        var sessionType = await _sessionTypeRepository.GetByIdAsync(request.SessionTypeId);
        if (sessionType == null)
            return NotFound<CourseHourlyRatePreviewDto>("Session type not found");

        var domainId = teacherSubject.Subject?.DomainId;
        if (domainId is null or <= 0)
            return BadRequest<CourseHourlyRatePreviewDto>("Subject domain is required for pricing.");

        var market = await _marketResolver.ResolveForUserAsync(request.UserId, cancellationToken);
        var estimateMinutes = request.TotalMinutes is > 0 ? request.TotalMinutes.Value : 60;

        PriceEstimate estimate;
        try
        {
            estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
            {
                DomainId = domainId.Value,
                SessionTypeCode = sessionType.Code,
                MarketCode = market.MarketCode,
                TotalMinutes = estimateMinutes,
                TeacherId = teacher.Id
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<CourseHourlyRatePreviewDto>(ex.Message);
        }

        var platformPricePerHour = await _pricingEngine.ResolvePricePerHourAsync(
            domainId.Value,
            sessionType.Code,
            market.MarketCode,
            cancellationToken: cancellationToken);

        var domainPricing = await _domainPricingRepository.GetByTeacherAndDomainAsync(
            teacher.Id,
            domainId.Value,
            cancellationToken);

        var hasCompletedInterview = domainPricing?.HasCompletedInterviewSession == true;
        var levelSharePct = domainPricing?.TeacherLevel?.TeacherSharePct;
        var projection = PricingEngine.ProjectTeacherEarnings(estimate, domainPricing, estimateMinutes);
        var freeSessionDeduction = request.TotalMinutes is > 0
            ? PricingEngine.FreeFirstSessionTeacherDeduction(
                projection, request.FirstSessionMinutes ?? 0, request.TotalMinutes.Value)
            : 0m;
        var earningsBase = estimate.EarningsPricePerHour ?? estimate.PricePerHour;
        decimal? estimatedPackageTotal = request.TotalMinutes is > 0 ? estimate.TotalPrice : null;

        return Success(entity: new CourseHourlyRatePreviewDto
        {
            PricePerHour = estimate.PricePerHour,
            Currency = estimate.Currency,
            MarketCode = estimate.MarketCode,
            TotalMinutes = request.TotalMinutes,
            EstimatedPackageTotal = estimatedPackageTotal,
            EarningsPricePerHour = earningsBase,
            TeacherSharePct = estimate.TeacherSharePct,
            TeacherEarnings = estimate.TeacherEarnings,
            HasCompletedInterviewSession = hasCompletedInterview,
            LevelSharePct = levelSharePct,
            ProjectedSharePct = projection.SharePct,
            ProjectedTeacherEarnings = projection.EarningsTotal,
            FreeSessionTeacherDeduction = freeSessionDeduction,
            TeacherEarningsPerHour = projection.EarningsPerHour,
            ReflectCustomPriceToStudent = estimate.ReflectCustomPriceToStudent,
            IsCustomStudentRate = estimate.PricePerHour != platformPricePerHour,
        });
    }
}
