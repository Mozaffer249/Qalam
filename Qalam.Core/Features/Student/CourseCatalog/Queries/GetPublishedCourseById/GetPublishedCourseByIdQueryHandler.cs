using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCourseById;

public class GetPublishedCourseByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetPublishedCourseByIdQuery, Response<CourseCatalogDetailDto>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;
    private readonly IStudentCoursePriceResolver _coursePriceResolver;
    private readonly IPricingMarketResolver _marketResolver;
    private readonly IStudentRepository _studentRepository;
    private readonly IFreeSessionPolicyService _freeSessionPolicy;

    public GetPublishedCourseByIdQueryHandler(
        ICourseRepository courseRepository,
        IMapper mapper,
        IStudentCoursePriceResolver coursePriceResolver,
        IPricingMarketResolver marketResolver,
        IStudentRepository studentRepository,
        IFreeSessionPolicyService freeSessionPolicy,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _courseRepository = courseRepository;
        _mapper = mapper;
        _coursePriceResolver = coursePriceResolver;
        _marketResolver = marketResolver;
        _studentRepository = studentRepository;
        _freeSessionPolicy = freeSessionPolicy;
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
        dto.Price = await _coursePriceResolver.ResolveCourseTotalPriceAsync(
            course, request.UserId, cancellationToken);

        var isGroup = string.Equals(
            dto.SessionTypeCode,
            PricingDefaults.SessionTypeGroup,
            StringComparison.OrdinalIgnoreCase);
        var sessionCount = dto.SessionsCount
            ?? dto.Sessions?.Count
            ?? 0;

        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student != null
            && _freeSessionPolicy.IsEligiblePackage(isGroup, sessionCount)
            && await _freeSessionPolicy.IsStudentEligibleForFreeTrialAsync(student.Id, cancellationToken))
        {
            dto.IsFreeTrialEligible = true;
        }

        var totalMinutes = CourseDurationHelper.ResolveFixedTotalMinutes(course);
        var firstMinutes = FreeSessionPolicyService.ResolveFirstSessionMinutes(
            dto.Sessions?.OrderBy(s => s.SessionNumber).Select(s => (int?)s.DurationMinutes).FirstOrDefault(),
            dto.SessionDurationMinutes,
            totalMinutes > 0 ? totalMinutes : null,
            sessionCount);
        var hourly = FreeSessionPolicyService.DerivePricePerHour(dto.Price, totalMinutes);
        var (credit, amountDue) = FreeSessionPolicyService.BuildTeaserAmounts(
            dto.IsFreeTrialEligible, dto.Price, hourly, firstMinutes);
        dto.FreeSessionCredit = credit;
        dto.AmountDue = amountDue;

        return Success(entity: dto);
    }
}
