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

        var coursePriceResolver = new Mock<IStudentCoursePriceResolver>();
        coursePriceResolver
            .Setup(r => r.ResolveCourseTotalPriceAsync(
                course,
                42,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(200m);

        var handler = new GetPublishedCourseByIdQueryHandler(
            courseRepo.Object,
            mapper.Object,
            coursePriceResolver.Object,
            marketResolver.Object,
            CreateSharedLocalizer().Object);

        var response = await handler.Handle(
            new GetPublishedCourseByIdQuery { UserId = 42, Id = 2003 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(200m, response.Data!.Price);
        Assert.Equal("SAR", response.Data.Currency);
        Assert.Equal("sa", response.Data.MarketCode);
        coursePriceResolver.Verify(
            r => r.ResolveCourseTotalPriceAsync(course, 42, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
