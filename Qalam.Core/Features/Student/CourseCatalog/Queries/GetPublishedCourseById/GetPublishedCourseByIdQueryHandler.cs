using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCourseById;

public class GetPublishedCourseByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetPublishedCourseByIdQuery, Response<CourseCatalogDetailDto>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingMarketResolver _marketResolver;

    public GetPublishedCourseByIdQueryHandler(
        ICourseRepository courseRepository,
        IMapper mapper,
        IPricingEngine pricingEngine,
        IPricingMarketResolver marketResolver,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _courseRepository = courseRepository;
        _mapper = mapper;
        _pricingEngine = pricingEngine;
        _marketResolver = marketResolver;
    }

    public async Task<Response<CourseCatalogDetailDto>> Handle(
        GetPublishedCourseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdWithDetailsAsync(request.Id);
        if (course == null)
            return NotFound<CourseCatalogDetailDto>("Course not found.");
        if (course.Status != CourseStatus.Published || !course.IsActive)
            return NotFound<CourseCatalogDetailDto>("Course not found or not available.");

        var dto = _mapper.Map<CourseCatalogDetailDto>(course);
        var market = await _marketResolver.ResolveForUserAsync(request.UserId, cancellationToken);
        dto.Currency = market.Currency;
        dto.MarketCode = market.MarketCode;

        var sessionTypeCode = course.SessionType?.Code ?? "individual";
        try
        {
            var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
            {
                DomainId = course.DomainId,
                SessionTypeCode = sessionTypeCode,
                MarketCode = market.MarketCode,
                TotalMinutes = 60,
                TeacherId = course.TeacherId
            }, cancellationToken);
            dto.Price = estimate.PricePerHour;
        }
        catch (InvalidOperationException)
        {
            // Keep stored course price as fallback.
        }

        return Success(entity: dto);
    }
}
