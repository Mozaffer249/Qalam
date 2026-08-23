using Moq;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Entity.Teaching;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Tests;

public class StudentCoursePriceResolverTests
{
    private const int ViewerUserId = 42;
    private const int DomainId = 1;
    private const int TeacherId = 1012;

    private static Course CreateCourse(decimal hourlyPrice = 85m, int totalMinutes = 120) =>
        new()
        {
            TeacherId = TeacherId,
            Price = hourlyPrice,
            SessionType = new SessionType { Id = 1, Code = "individual" },
            TeacherSubject = new TeacherSubject
            {
                Subject = new Subject { Id = 3, DomainId = DomainId }
            },
            Sessions = totalMinutes switch
            {
                120 =>
                [
                    new CourseSession { DurationMinutes = 60 },
                    new CourseSession { DurationMinutes = 60 },
                ],
                _ => []
            }
        };

    private static (
        StudentCoursePriceResolver Resolver,
        Mock<IPricingEngine> PricingEngine,
        Mock<IPricingMarketResolver> MarketResolver)
        CreateSut()
    {
        var pricingEngine = new Mock<IPricingEngine>();
        var marketResolver = new Mock<IPricingMarketResolver>();
        marketResolver
            .Setup(r => r.ResolveForUserAsync(ViewerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPricingMarket
            {
                MarketCode = "sa",
                Currency = "SAR",
                NameEn = "Saudi Arabia",
                NameAr = "السعودية",
                Source = PricingMarketResolutionSource.Default
            });

        var resolver = new StudentCoursePriceResolver(
            pricingEngine.Object,
            marketResolver.Object);

        return (resolver, pricingEngine, marketResolver);
    }

    [Fact]
    public async Task ResolveCourseTotalPriceAsync_ReflectOn_UsesEstimateTotal()
    {
        var (resolver, pricingEngine, _) = CreateSut();
        var course = CreateCourse();

        pricingEngine
            .Setup(e => e.EstimateAsync(
                It.Is<PricingEstimateRequest>(r =>
                    r.DomainId == DomainId
                    && r.SessionTypeCode == "individual"
                    && r.MarketCode == "sa"
                    && r.TotalMinutes == 120
                    && r.TeacherId == TeacherId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEstimate(
                PricePerHour: 100m,
                TotalMinutes: 120,
                TotalPrice: 200m,
                TeacherSharePct: 70m,
                TeacherEarnings: 140m,
                PlatformShare: 60m,
                DomainSessionPriceId: 1,
                TeacherLevelId: 1,
                MarketCode: "sa",
                Currency: "SAR",
                ReflectCustomPriceToStudent: true,
                EarningsPricePerHour: 100m));

        var total = await resolver.ResolveCourseTotalPriceAsync(course, ViewerUserId);

        Assert.Equal(200m, total);
    }

    [Fact]
    public async Task ResolveCourseTotalPriceAsync_ReflectOff_UsesPlatformEstimateTotal()
    {
        var (resolver, pricingEngine, _) = CreateSut();
        var course = CreateCourse(hourlyPrice: 100m);

        pricingEngine
            .Setup(e => e.EstimateAsync(It.IsAny<PricingEstimateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEstimate(
                PricePerHour: 85m,
                TotalMinutes: 120,
                TotalPrice: 170m,
                TeacherSharePct: 70m,
                TeacherEarnings: 119m,
                PlatformShare: 51m,
                DomainSessionPriceId: 1,
                TeacherLevelId: 1,
                MarketCode: "sa",
                Currency: "SAR",
                ReflectCustomPriceToStudent: false,
                EarningsPricePerHour: 100m));

        var total = await resolver.ResolveCourseTotalPriceAsync(course, ViewerUserId);

        Assert.Equal(170m, total);
    }

    [Fact]
    public async Task ResolveEnrollmentCoursePriceAsync_PrefersPricingSnapshot()
    {
        var (resolver, pricingEngine, _) = CreateSut();
        var enrollment = new Enrollment
        {
            PricingSnapshot = new PricingSnapshot { TotalPrice = 200m },
            Course = CreateCourse(),
            AmountDue = 170m,
        };

        var total = await resolver.ResolveEnrollmentCoursePriceAsync(enrollment, ViewerUserId);

        Assert.Equal(200m, total);
        pricingEngine.Verify(
            e => e.EstimateAsync(It.IsAny<PricingEstimateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveEnrollmentCoursePriceAsync_UsesRequestEstimateWhenNoSnapshot()
    {
        var (resolver, pricingEngine, _) = CreateSut();
        var enrollment = new Enrollment
        {
            EnrollmentRequest = new CourseEnrollmentRequest { EstimatedTotalPrice = 170m },
            Course = CreateCourse(),
        };

        var total = await resolver.ResolveEnrollmentCoursePriceAsync(enrollment, ViewerUserId);

        Assert.Equal(170m, total);
        pricingEngine.Verify(
            e => e.EstimateAsync(It.IsAny<PricingEstimateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void ResolveEnrollmentPayableAmount_UsesAmountDueWhenNoSnapshotOrRequest()
    {
        var (resolver, pricingEngine, _) = CreateSut();
        var enrollment = new Enrollment { Course = CreateCourse(), AmountDue = 170m };

        var payable = resolver.ResolveEnrollmentPayableAmount(enrollment);

        Assert.Equal(170m, payable);
        pricingEngine.Verify(
            e => e.EstimateAsync(It.IsAny<PricingEstimateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void ResolveEnrollmentPayableAmount_PriorityChain()
    {
        var (resolver, _, _) = CreateSut();

        var withSnapshot = new Enrollment
        {
            PricingSnapshot = new PricingSnapshot { TotalPrice = 200m },
            EnrollmentRequest = new CourseEnrollmentRequest { EstimatedTotalPrice = 170m },
            AmountDue = 150m
        };
        Assert.Equal(200m, resolver.ResolveEnrollmentPayableAmount(withSnapshot));

        var withRequestOnly = new Enrollment
        {
            EnrollmentRequest = new CourseEnrollmentRequest { EstimatedTotalPrice = 170m },
            AmountDue = 150m
        };
        Assert.Equal(170m, resolver.ResolveEnrollmentPayableAmount(withRequestOnly));

        var amountDueOnly = new Enrollment { AmountDue = 150m };
        Assert.Equal(150m, resolver.ResolveEnrollmentPayableAmount(amountDueOnly));
    }
}
