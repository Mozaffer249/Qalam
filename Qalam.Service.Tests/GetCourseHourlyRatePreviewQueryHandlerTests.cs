using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.Features.Teacher.Pricing.Queries.GetCourseHourlyRatePreview;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using TeacherEntity = Qalam.Data.Entity.Teacher.Teacher;

namespace Qalam.Service.Tests;

public class GetCourseHourlyRatePreviewQueryHandlerTests
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
    public async Task Handle_ReturnsNotFound_WhenTeacherSubjectNotOwned()
    {
        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new TeacherEntity { Id = 5, UserId = 10 });

        var subjectRepo = new Mock<ITeacherSubjectRepository>();
        subjectRepo
            .Setup(r => r.GetByIdForTeacherAsync(5, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeacherSubject?)null);

        var handler = new GetCourseHourlyRatePreviewQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            subjectRepo.Object,
            Mock.Of<ISessionTypeRepository>(),
            Mock.Of<IPricingEngine>(),
            Mock.Of<IPricingMarketResolver>());

        var response = await handler.Handle(
            new GetCourseHourlyRatePreviewQuery
            {
                UserId = 10,
                TeacherSubjectId = 99,
                SessionTypeId = 1,
            },
            CancellationToken.None);

        Assert.False(response.Succeeded);
    }

    [Fact]
    public async Task Handle_ReturnsPreview_WithPackageTotal()
    {
        var teacherSubject = new TeacherSubject
        {
            Id = 12,
            TeacherId = 5,
            Subject = new Subject { Id = 3, DomainId = 7 },
        };

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new TeacherEntity { Id = 5, UserId = 10 });

        var subjectRepo = new Mock<ITeacherSubjectRepository>();
        subjectRepo
            .Setup(r => r.GetByIdForTeacherAsync(5, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacherSubject);

        var sessionTypeRepo = new Mock<ISessionTypeRepository>();
        sessionTypeRepo
            .Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(new SessionType { Id = 2, Code = "individual" });

        var marketResolver = new Mock<IPricingMarketResolver>();
        marketResolver
            .Setup(r => r.ResolveForUserAsync(10, It.IsAny<CancellationToken>()))
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
            .Setup(e => e.ResolvePricePerHourAsync(7, "individual", "sa", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100m);

        var handler = new GetCourseHourlyRatePreviewQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            subjectRepo.Object,
            sessionTypeRepo.Object,
            pricingEngine.Object,
            marketResolver.Object);

        var response = await handler.Handle(
            new GetCourseHourlyRatePreviewQuery
            {
                UserId = 10,
                TeacherSubjectId = 12,
                SessionTypeId = 2,
                TotalMinutes = 120,
            },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(100m, response.Data!.PricePerHour);
        Assert.Equal("SAR", response.Data.Currency);
        Assert.Equal("sa", response.Data.MarketCode);
        Assert.Equal(120, response.Data.TotalMinutes);
        Assert.Equal(200m, response.Data.EstimatedPackageTotal);
    }
}
