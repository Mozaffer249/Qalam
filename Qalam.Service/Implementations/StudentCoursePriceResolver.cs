using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Implementations;

public class StudentCoursePriceResolver : IStudentCoursePriceResolver
{
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingMarketResolver _marketResolver;

    public StudentCoursePriceResolver(
        IPricingEngine pricingEngine,
        IPricingMarketResolver marketResolver)
    {
        _pricingEngine = pricingEngine;
        _marketResolver = marketResolver;
    }

    public async Task<decimal> ResolveCourseTotalPriceAsync(
        Course course,
        int viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var totalMinutes = CourseDurationHelper.ResolveFixedTotalMinutes(course);
        var domainId = course.TeacherSubject?.Subject?.DomainId ?? 0;
        var sessionTypeCode = course.SessionType?.Code ?? "individual";

        return await ResolveCourseTotalPriceAsync(
            domainId,
            sessionTypeCode,
            course.TeacherId,
            totalMinutes,
            course.Price,
            viewerUserId,
            cancellationToken);
    }

    public async Task<decimal> ResolveCourseTotalPriceAsync(
        int domainId,
        string sessionTypeCode,
        int teacherId,
        int totalMinutes,
        decimal storedHourlyPrice,
        int viewerUserId,
        CancellationToken cancellationToken = default)
    {
        if (totalMinutes <= 0)
            return storedHourlyPrice;

        if (domainId <= 0)
            return CourseDurationHelper.ComputeTotalPriceFromHourly(storedHourlyPrice, totalMinutes);

        var market = await _marketResolver.ResolveForUserAsync(viewerUserId, cancellationToken);

        try
        {
            var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
            {
                DomainId = domainId,
                SessionTypeCode = sessionTypeCode,
                MarketCode = market.MarketCode,
                TotalMinutes = totalMinutes,
                TeacherId = teacherId
            }, cancellationToken);

            return estimate.TotalPrice;
        }
        catch (InvalidOperationException)
        {
            return CourseDurationHelper.ComputeTotalPriceFromHourly(storedHourlyPrice, totalMinutes);
        }
    }

    public async Task<decimal> ResolveEnrollmentCoursePriceAsync(
        Enrollment enrollment,
        int viewerUserId,
        CancellationToken cancellationToken = default)
    {
        if (enrollment.PricingSnapshot is { TotalPrice: > 0 } snapshot)
            return snapshot.TotalPrice;

        if (enrollment.EnrollmentRequest is { EstimatedTotalPrice: > 0 } request)
            return request.EstimatedTotalPrice;

        if (enrollment.AmountDue > 0)
            return enrollment.AmountDue;

        var course = enrollment.Course;
        if (course == null)
            return 0;

        return await ResolveCourseTotalPriceAsync(course, viewerUserId, cancellationToken);
    }
}
