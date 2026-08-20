using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Pricing.Queries.GetCourseHourlyRatePreview;

public class GetCourseHourlyRatePreviewQueryHandler : ResponseHandler,
    IRequestHandler<GetCourseHourlyRatePreviewQuery, Response<CourseHourlyRatePreviewDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ISessionTypeRepository _sessionTypeRepository;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingMarketResolver _marketResolver;

    public GetCourseHourlyRatePreviewQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        ISessionTypeRepository sessionTypeRepository,
        IPricingEngine pricingEngine,
        IPricingMarketResolver marketResolver) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _sessionTypeRepository = sessionTypeRepository;
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
        var pricePerHour = await _pricingEngine.ResolvePricePerHourAsync(
            domainId.Value,
            sessionType.Code,
            market.MarketCode,
            cancellationToken: cancellationToken);

        decimal? estimatedPackageTotal = null;
        if (request.TotalMinutes is > 0)
        {
            estimatedPackageTotal = Math.Round(
                (request.TotalMinutes.Value / 60m) * pricePerHour,
                2,
                MidpointRounding.AwayFromZero);
        }

        return Success(entity: new CourseHourlyRatePreviewDto
        {
            PricePerHour = pricePerHour,
            Currency = market.Currency,
            MarketCode = market.MarketCode,
            TotalMinutes = request.TotalMinutes,
            EstimatedPackageTotal = estimatedPackageTotal,
        });
    }
}
