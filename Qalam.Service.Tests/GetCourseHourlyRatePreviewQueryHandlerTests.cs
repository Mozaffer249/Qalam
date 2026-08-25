using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.Features.Teacher.Pricing.Queries.GetCourseHourlyRatePreview;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;
using TeacherEntity = Qalam.Data.Entity.Teacher.Teacher;
using TeacherLevel = Qalam.Data.Entity.Teacher.TeacherLevel;

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
            Mock.Of<ITeacherDomainPricingRepository>(),
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
    public async Task Handle_ReturnsPreview_WithPackageTotal_FromEstimate()
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
            .Setup(e => e.EstimateAsync(
                It.Is<PricingEstimateRequest>(r =>
                    r.DomainId == 7
                    && r.SessionTypeCode == "individual"
                    && r.TotalMinutes == 120
                    && r.TeacherId == 5),
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
        pricingEngine
            .Setup(e => e.ResolvePricePerHourAsync(7, "individual", "sa", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(85m);

        var domainPricingRepo = new Mock<ITeacherDomainPricingRepository>();
        domainPricingRepo
            .Setup(r => r.GetByTeacherAndDomainAsync(5, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherDomainPricing
            {
                TeacherId = 5,
                DomainId = 7,
                HasCompletedInterviewSession = true,
                TeacherLevel = new TeacherLevel { TeacherSharePct = 70m },
            });

        var handler = new GetCourseHourlyRatePreviewQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            subjectRepo.Object,
            sessionTypeRepo.Object,
            domainPricingRepo.Object,
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
        Assert.True(response.Data.IsCustomStudentRate);
        Assert.True(response.Data.ReflectCustomPriceToStudent);
        Assert.True(response.Data.HasCompletedInterviewSession);
        Assert.Equal(70m, response.Data.LevelSharePct);
        Assert.Equal(70m, response.Data.ProjectedSharePct);
        Assert.Equal(140m, response.Data.ProjectedTeacherEarnings);
    }

    [Fact]
    public async Task Handle_InterviewPending_ReturnsProjectedShareFromLevel()
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
            .Setup(e => e.EstimateAsync(It.IsAny<PricingEstimateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEstimate(
                PricePerHour: 85m,
                TotalMinutes: 60,
                TotalPrice: 85m,
                TeacherSharePct: 0m,
                TeacherEarnings: 0m,
                PlatformShare: 85m,
                DomainSessionPriceId: 1,
                TeacherLevelId: 1,
                MarketCode: "sa",
                Currency: "SAR",
                ReflectCustomPriceToStudent: false,
                EarningsPricePerHour: 85m));
        pricingEngine
            .Setup(e => e.ResolvePricePerHourAsync(7, "individual", "sa", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(85m);

        var domainPricingRepo = new Mock<ITeacherDomainPricingRepository>();
        domainPricingRepo
            .Setup(r => r.GetByTeacherAndDomainAsync(5, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherDomainPricing
            {
                TeacherId = 5,
                DomainId = 7,
                HasCompletedInterviewSession = false,
                TeacherLevel = new TeacherLevel { TeacherSharePct = 60m },
            });

        var handler = new GetCourseHourlyRatePreviewQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            subjectRepo.Object,
            sessionTypeRepo.Object,
            domainPricingRepo.Object,
            pricingEngine.Object,
            marketResolver.Object);

        var response = await handler.Handle(
            new GetCourseHourlyRatePreviewQuery
            {
                UserId = 10,
                TeacherSubjectId = 12,
                SessionTypeId = 2,
            },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(0m, response.Data!.TeacherSharePct);
        Assert.False(response.Data.HasCompletedInterviewSession);
        Assert.Equal(60m, response.Data.LevelSharePct);
        Assert.Equal(60m, response.Data.ProjectedSharePct);
        Assert.Equal(51m, response.Data.ProjectedTeacherEarnings);
    }
}
