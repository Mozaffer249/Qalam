using AutoMapper;
using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCourseById;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Tests;

public class GetPublishedCourseByIdQueryHandlerPricingTests
{
    private static Mock<IStringLocalizer<SharedResources>> CreateSharedLocalizer()
    {
        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        return localizer;
    }

    [Fact]
    public async Task Handle_UsesEstimateAsync_TotalCoursePrice()
    {
        var course = new Course
        {
            Id = 2003,
            TeacherId = 1012,
            Status = CourseStatus.Published,
            IsActive = true,
            Price = 85m,
            SessionType = new SessionType { Id = 1, Code = "individual" },
            TeacherSubject = new TeacherSubject
            {
                Subject = new Subject { Id = 3, DomainId = 1 }
            },
            Sessions = new List<CourseSession>
            {
                new() { DurationMinutes = 60 },
                new() { DurationMinutes = 60 },
            }
        };

        var courseRepo = new Mock<ICourseRepository>();
        courseRepo.Setup(r => r.GetByIdWithDetailsAsync(2003)).ReturnsAsync(course);

        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<CourseCatalogDetailDto>(course))
            .Returns(new CourseCatalogDetailDto { Id = 2003, Price = 85m, DomainId = 1 });

        var marketResolver = new Mock<IPricingMarketResolver>();
        marketResolver
            .Setup(r => r.ResolveForUserAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPricingMarket
            {
                MarketCode = "sa",
                Currency = "SAR",
                NameEn = "Saudi Arabia",
                NameAr = "السعودية",
                Source = PricingMarketResolutionSource.Default
            });

        var pricingEngine = new Mock<IPricingEngine>();
        pricingEngine
            .Setup(e => e.EstimateAsync(
                It.Is<PricingEstimateRequest>(r =>
                    r.DomainId == 1
                    && r.SessionTypeCode == "individual"
                    && r.MarketCode == "sa"
                    && r.TotalMinutes == 120
                    && r.TeacherId == 1012),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEstimate(
                PricePerHour: 100m,
                TotalMinutes: 120,
                TotalPrice: 200m,
                TeacherSharePct: 70m,
                TeacherEarnings: 70m,
                PlatformShare: 30m,
                DomainSessionPriceId: 1,
                TeacherLevelId: 1,
                MarketCode: "sa",
                Currency: "SAR",
                ReflectCustomPriceToStudent: true,
                EarningsPricePerHour: 100m));

        var handler = new GetPublishedCourseByIdQueryHandler(
            courseRepo.Object,
            mapper.Object,
            pricingEngine.Object,
            marketResolver.Object,
            CreateSharedLocalizer().Object);

        var response = await handler.Handle(
            new GetPublishedCourseByIdQuery { UserId = 42, Id = 2003 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(200m, response.Data!.Price);
        Assert.Equal("SAR", response.Data.Currency);
        Assert.Equal("sa", response.Data.MarketCode);
        pricingEngine.Verify(
            e => e.ResolvePricePerHourAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
