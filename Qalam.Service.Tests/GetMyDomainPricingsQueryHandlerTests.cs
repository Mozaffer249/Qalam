using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.Features.Teacher.Pricing.Queries.GetMyDomainPricings;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;
using TeacherEntity = Qalam.Data.Entity.Teacher.Teacher;
using TeacherLevel = Qalam.Data.Entity.Teacher.TeacherLevel;

namespace Qalam.Service.Tests;

public class GetMyDomainPricingsQueryHandlerTests
{
    private const int UserId = 10;
    private const int TeacherId = 5;
    private const int DomainId = 7;

    private static GetMyDomainPricingsQueryHandler CreateHandler(
        List<DomainSessionPrice> platformRates,
        TeacherLevel? starterLevel)
    {
        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(UserId))
            .ReturnsAsync(new TeacherEntity { Id = TeacherId, UserId = UserId });

        var pricingRepo = new Mock<ITeacherDomainPricingRepository>();
        pricingRepo
            .Setup(r => r.ListByTeacherAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeacherDomainPricing>());

        var priceRepo = new Mock<IDomainSessionPriceRepository>();
        priceRepo
            .Setup(r => r.ListCurrentRatesAsync("sa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platformRates);

        var marketResolver = new Mock<IPricingMarketResolver>();
        marketResolver
            .Setup(r => r.ResolveForUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPricingMarket
            {
                MarketCode = "sa",
                Currency = "SAR",
                NameEn = "Saudi Arabia",
                NameAr = "السعودية",
                Source = PricingMarketResolutionSource.Default
            });

        var approvalRepo = new Mock<ITeacherDomainApprovalRepository>();
        approvalRepo
            .Setup(r => r.GetByTeacherAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeacherDomainApproval>
            {
                new() { TeacherId = TeacherId, DomainId = DomainId },
            });

        var domainRepo = new Mock<IEducationDomainRepository>();
        domainRepo
            .Setup(r => r.GetDomainDtoByIdAsync(DomainId))
            .ReturnsAsync(new EducationDomainDto
            {
                Id = DomainId,
                Code = "school",
                NameEn = "School education",
                NameAr = "التعليم المدرسي",
            });

        var levelRepo = new Mock<ITeacherLevelRepository>();
        levelRepo
            .Setup(r => r.GetStarterLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(starterLevel);

        return new GetMyDomainPricingsQueryHandler(
            localizer.Object,
            teacherRepo.Object,
            pricingRepo.Object,
            priceRepo.Object,
            marketResolver.Object,
            Mock.Of<IPricingMarketRepository>(),
            approvalRepo.Object,
            domainRepo.Object,
            levelRepo.Object);
    }

    [Fact]
    public async Task Handle_ApprovedDomainWithoutPricingRow_UsesStarterLevelShare()
    {
        var handler = CreateHandler(
            new List<DomainSessionPrice>
            {
                new() { DomainId = DomainId, SessionTypeCode = "individual", PricePerHour = 50m },
                new() { DomainId = DomainId, SessionTypeCode = "group", PricePerHour = 30m },
            },
            new TeacherLevel { TeacherSharePct = 30m });

        var response = await handler.Handle(
            new GetMyDomainPricingsQuery { UserId = UserId },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        var row = Assert.Single(response.Data!);
        Assert.Equal(DomainId, row.DomainId);
        Assert.Equal("التعليم المدرسي", row.DomainNameAr);
        Assert.Equal(TeacherPriceStatus.Ready, row.PriceStatus);
        Assert.Equal(15m, row.IndividualTeacherEarningsPerHour);
        Assert.Equal(9m, row.GroupTeacherEarningsPerHour);
    }

    [Fact]
    public async Task Handle_NoPlatformRate_ReturnsNoPlatformRateStatus()
    {
        var handler = CreateHandler(
            new List<DomainSessionPrice>(),
            new TeacherLevel { TeacherSharePct = 30m });

        var response = await handler.Handle(
            new GetMyDomainPricingsQuery { UserId = UserId },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        var row = Assert.Single(response.Data!);
        Assert.Equal(TeacherPriceStatus.NoPlatformRate, row.PriceStatus);
        Assert.Null(row.IndividualTeacherEarningsPerHour);
        Assert.Null(row.GroupTeacherEarningsPerHour);
    }
}
