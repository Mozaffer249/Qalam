using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Tests;

public class PricingEngineTests
{
    private const int DomainId = 1;
    private const int TeacherId = 10;
    private const string SessionType = "individual";
    private const string MarketCode = "sa";
    private const string Currency = "SAR";

    private static DomainSessionPrice CreateRate(
        decimal pricePerHour = 100m,
        int id = 5,
        string sessionTypeCode = SessionType) =>
        new()
        {
            Id = id,
            MarketCode = MarketCode,
            DomainId = DomainId,
            SessionTypeCode = sessionTypeCode,
            PricePerHour = pricePerHour,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        };

    private static PricingMarket CreateMarket(
        string code = MarketCode,
        string currency = Currency,
        decimal exchangeRateFromBase = 1m) =>
        new()
        {
            Code = code,
            Currency = currency,
            NameEn = "Saudi Arabia",
            NameAr = "السعودية",
            IsActive = true,
            IsDefault = code == MarketCode,
            ExchangeRateFromBase = exchangeRateFromBase
        };

    private static Teacher CreateTeacher() =>
        new()
        {
            Id = TeacherId,
            HasCompletedInterviewSession = true
        };

    private static TeacherDomainPricing CreateDomainPricing(
        decimal? customShare = null,
        decimal levelShare = 60m,
        int levelId = 1,
        bool interviewDone = true,
        decimal? customIndividualPrice = null,
        decimal? customGroupPrice = null,
        bool reflectIndividual = false,
        bool reflectGroup = false)
    {
        var level = interviewDone
            ? new TeacherLevel
            {
                Id = levelId,
                Code = "starter",
                NameAr = "مبتدئ",
                NameEn = "Starter",
                OrderIndex = 1,
                TeacherSharePct = levelShare,
                IsActive = true
            }
            : null;

        return new TeacherDomainPricing
        {
            TeacherId = TeacherId,
            DomainId = DomainId,
            TeacherLevelId = interviewDone ? levelId : null,
            TeacherLevel = level,
            CustomTeacherSharePct = customShare,
            CustomIndividualPricePerHour = customIndividualPrice,
            CustomGroupPricePerHour = customGroupPrice,
            ReflectCustomIndividualPriceToStudent = reflectIndividual,
            ReflectCustomGroupPriceToStudent = reflectGroup,
            HasCompletedInterviewSession = interviewDone
        };
    }

    private static (
        PricingEngine Engine,
        Mock<IDomainSessionPriceRepository> PriceRepo,
        Mock<ITeacherRepository> TeacherRepo,
        Mock<ITeacherDomainPricingRepository> DomainPricingRepo)
        CreateSut(
            Teacher? teacher = null,
            TeacherDomainPricing? domainPricing = null,
            DomainSessionPrice? rate = null,
            bool missingRate = false,
            PricingMarket? market = null)
    {
        var priceRepo = new Mock<IDomainSessionPriceRepository>();
        priceRepo
            .Setup(r => r.GetEffectiveRateAsync(DomainId, SessionType, MarketCode, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(missingRate ? null : (rate ?? CreateRate()));

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo
            .Setup(r => r.GetByIdAsync(TeacherId))
            .ReturnsAsync(teacher ?? CreateTeacher());

        var domainPricingRepo = new Mock<ITeacherDomainPricingRepository>();
        domainPricingRepo
            .Setup(r => r.GetByTeacherAndDomainAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainPricing ?? CreateDomainPricing());

        var marketRepo = new Mock<IPricingMarketRepository>();
        marketRepo
            .Setup(r => r.GetByCodeAsync(MarketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(market ?? CreateMarket());

        var engine = new PricingEngine(
            priceRepo.Object,
            teacherRepo.Object,
            domainPricingRepo.Object,
            marketRepo.Object);
        return (engine, priceRepo, teacherRepo, domainPricingRepo);
    }

    private static PricingEstimateRequest CreateRequest(int totalMinutes) => new()
    {
        DomainId = DomainId,
        SessionTypeCode = SessionType,
        MarketCode = MarketCode,
        TotalMinutes = totalMinutes,
        TeacherId = TeacherId
    };

    [Fact]
    public async Task EstimateAsync_TwoHoursAt100PerHourWith60PctShare_SplitsCorrectly()
    {
        var (engine, _, _, _) = CreateSut();

        var estimate = await engine.EstimateAsync(CreateRequest(120));

        Assert.Equal(100m, estimate.PricePerHour);
        Assert.Equal(MarketCode, estimate.MarketCode);
        Assert.Equal(Currency, estimate.Currency);
        Assert.Equal(120, estimate.TotalMinutes);
        Assert.Equal(200m, estimate.TotalPrice);
        Assert.Equal(60m, estimate.TeacherSharePct);
        Assert.Equal(120m, estimate.TeacherEarnings);
        Assert.Equal(80m, estimate.PlatformShare);
        Assert.Equal(1, estimate.TeacherLevelId);
    }

    [Fact]
    public async Task EstimateAsync_DifferentMarkets_ReturnDifferentCurrency()
    {
        var egRate = CreateRate(800m);
        egRate.MarketCode = "eg";
        var priceRepo = new Mock<IDomainSessionPriceRepository>();
        priceRepo
            .Setup(r => r.GetEffectiveRateAsync(DomainId, SessionType, "eg", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(egRate);

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(TeacherId)).ReturnsAsync(CreateTeacher());

        var domainPricingRepo = new Mock<ITeacherDomainPricingRepository>();
        domainPricingRepo
            .Setup(r => r.GetByTeacherAndDomainAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDomainPricing());

        var marketRepo = new Mock<IPricingMarketRepository>();
        marketRepo.Setup(r => r.GetByCodeAsync("eg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMarket("eg", "EGP", 8m));

        var engine = new PricingEngine(
            priceRepo.Object, teacherRepo.Object, domainPricingRepo.Object, marketRepo.Object);

        var estimate = await engine.EstimateAsync(new PricingEstimateRequest
        {
            DomainId = DomainId,
            SessionTypeCode = SessionType,
            MarketCode = "eg",
            TotalMinutes = 60,
            TeacherId = TeacherId
        });

        Assert.Equal("EGP", estimate.Currency);
        Assert.Equal("eg", estimate.MarketCode);
        Assert.Equal(800m, estimate.TotalPrice);
    }

    [Fact]
    public async Task EstimateAsync_45Minutes_RoundsAwayFromZero()
    {
        var (engine, _, _, _) = CreateSut(rate: CreateRate(100m));

        var estimate = await engine.EstimateAsync(CreateRequest(45));

        Assert.Equal(75m, estimate.TotalPrice);
        Assert.Equal(45m, estimate.TeacherEarnings);
        Assert.Equal(30m, estimate.PlatformShare);
    }

    [Fact]
    public async Task EstimateAsync_UsesCustomTeacherShareOverride_WhenSet()
    {
        var (engine, _, _, _) = CreateSut(domainPricing: CreateDomainPricing(customShare: 85m, levelShare: 60m));

        var estimate = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(85m, estimate.TeacherSharePct);
        Assert.Equal(85m, estimate.TeacherEarnings);
        Assert.Equal(15m, estimate.PlatformShare);
    }

    [Fact]
    public async Task EstimateAsync_UsesTeacherLevelShare_WhenNoOverride()
    {
        var (engine, _, _, _) = CreateSut(domainPricing: CreateDomainPricing(levelShare: 80m, levelId: 3));

        var estimate = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(80m, estimate.TeacherSharePct);
        Assert.Equal(80m, estimate.TeacherEarnings);
        Assert.Equal(20m, estimate.PlatformShare);
        Assert.Equal(3, estimate.TeacherLevelId);
    }

    [Fact]
    public async Task EstimateAsync_ReflectOn_StudentPaysTeacherCustomPrice()
    {
        var (engine, _, _, _) = CreateSut(
            domainPricing: CreateDomainPricing(customIndividualPrice: 150m, reflectIndividual: true, levelShare: 60m),
            rate: CreateRate(100m));

        var estimate = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(150m, estimate.PricePerHour);
        Assert.Equal(150m, estimate.TotalPrice);
        Assert.Equal(90m, estimate.TeacherEarnings);
        Assert.Equal(60m, estimate.PlatformShare);
        Assert.True(estimate.ReflectCustomPriceToStudent);
        Assert.Equal(150m, estimate.EarningsPricePerHour);
    }

    [Fact]
    public async Task EstimateAsync_ReflectOff_StudentPaysPlatform_EarningsFromTeacherPrice()
    {
        var (engine, _, _, _) = CreateSut(
            domainPricing: CreateDomainPricing(customIndividualPrice: 150m, reflectIndividual: false, levelShare: 60m),
            rate: CreateRate(100m));

        var estimate = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(100m, estimate.PricePerHour);
        Assert.Equal(100m, estimate.TotalPrice);
        Assert.Equal(90m, estimate.TeacherEarnings);
        Assert.Equal(10m, estimate.PlatformShare);
        Assert.False(estimate.ReflectCustomPriceToStudent);
        Assert.Equal(150m, estimate.EarningsPricePerHour);
    }

    [Fact]
    public async Task EstimateAsync_GroupReflectOn_UsesGroupCustomPriceOnly()
    {
        const string groupSession = "group";
        var priceRepo = new Mock<IDomainSessionPriceRepository>();
        priceRepo
            .Setup(r => r.GetEffectiveRateAsync(DomainId, groupSession, MarketCode, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRate(100m, sessionTypeCode: groupSession));

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(TeacherId)).ReturnsAsync(CreateTeacher());

        var domainPricingRepo = new Mock<ITeacherDomainPricingRepository>();
        domainPricingRepo
            .Setup(r => r.GetByTeacherAndDomainAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDomainPricing(
                customIndividualPrice: 120m,
                customGroupPrice: 80m,
                reflectIndividual: false,
                reflectGroup: true,
                levelShare: 50m));

        var marketRepo = new Mock<IPricingMarketRepository>();
        marketRepo.Setup(r => r.GetByCodeAsync(MarketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMarket());

        var engine = new PricingEngine(
            priceRepo.Object, teacherRepo.Object, domainPricingRepo.Object, marketRepo.Object);

        var estimate = await engine.EstimateAsync(new PricingEstimateRequest
        {
            DomainId = DomainId,
            SessionTypeCode = groupSession,
            MarketCode = MarketCode,
            TotalMinutes = 60,
            TeacherId = TeacherId
        });

        Assert.Equal(80m, estimate.PricePerHour);
        Assert.True(estimate.ReflectCustomPriceToStudent);
        Assert.Equal(40m, estimate.TeacherEarnings);
    }

    [Fact]
    public async Task EstimateAsync_ZeroMinutes_Throws()
    {
        var (engine, _, _, _) = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine.EstimateAsync(CreateRequest(0)));
    }

    [Fact]
    public async Task EstimateAsync_NoRate_Throws()
    {
        var (engine, _, _, _) = CreateSut(missingRate: true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine.EstimateAsync(CreateRequest(60)));

        Assert.Contains("No active pricing rule", ex.Message);
    }

    [Fact]
    public async Task EstimateAsync_TeacherNotFound_Throws()
    {
        var (engine, _, teacherRepo, _) = CreateSut();
        teacherRepo
            .Setup(r => r.GetByIdAsync(TeacherId))
            .ReturnsAsync((Teacher?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine.EstimateAsync(CreateRequest(60)));

        Assert.Contains($"Teacher {TeacherId} not found", ex.Message);
    }

    [Fact]
    public async Task EstimateAsync_TeacherNoLevel_UsesZeroShare()
    {
        var (engine, _, _, _) = CreateSut(domainPricing: CreateDomainPricing(interviewDone: false));

        var result = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(0m, result.TeacherSharePct);
        Assert.Equal(0m, result.TeacherEarnings);
        Assert.Equal(100m, result.TotalPrice);
    }

    [Fact]
    public async Task ResolvePricePerHourAsync_ReturnsEffectiveRate()
    {
        var (engine, _, _, _) = CreateSut(rate: CreateRate(150m));

        var price = await engine.ResolvePricePerHourAsync(DomainId, SessionType, MarketCode);

        Assert.Equal(150m, price);
    }

    [Fact]
    public async Task CreateSnapshotAsync_CopiesEstimateFields()
    {
        var (engine, _, _, _) = CreateSut();

        var snapshot = await engine.CreateSnapshotAsync(new CreatePricingSnapshotRequest
        {
            Context = PricingSnapshotContext.OpenSessionOffer,
            ContextEntityId = 99,
            DomainId = DomainId,
            SessionTypeCode = SessionType,
            MarketCode = MarketCode,
            TotalMinutes = 120,
            TeacherId = TeacherId
        });

        Assert.Equal(PricingSnapshotContext.OpenSessionOffer, snapshot.Context);
        Assert.Equal(99, snapshot.ContextEntityId);
        Assert.Equal(DomainId, snapshot.DomainId);
        Assert.Equal(SessionType, snapshot.SessionTypeCode);
        Assert.Equal(MarketCode, snapshot.MarketCode);
        Assert.Equal(Currency, snapshot.Currency);
        Assert.Equal(TeacherId, snapshot.TeacherId);
        Assert.Equal(200m, snapshot.TotalPrice);
        Assert.Equal(120m, snapshot.TeacherEarnings);
        Assert.Equal(80m, snapshot.PlatformShare);
        Assert.Equal(5, snapshot.DomainSessionPriceId);
    }

    [Fact]
    public async Task EstimateAsync_InterviewPendingTeacher_UsesZeroShare()
    {
        var (engine, _, _, _) = CreateSut(domainPricing: CreateDomainPricing(interviewDone: false));

        var result = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(0m, result.TeacherSharePct);
        Assert.Equal(0m, result.TeacherEarnings);
        Assert.Equal(100m, result.TotalPrice);
        Assert.Equal(100m, result.PlatformShare);
    }

    [Fact]
    public async Task EstimateAsync_MissingDomainPricing_UsesZeroShare()
    {
        var priceRepo = new Mock<IDomainSessionPriceRepository>();
        priceRepo
            .Setup(r => r.GetEffectiveRateAsync(DomainId, SessionType, MarketCode, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRate());

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(TeacherId)).ReturnsAsync(CreateTeacher());

        var domainPricingRepo = new Mock<ITeacherDomainPricingRepository>();
        domainPricingRepo
            .Setup(r => r.GetByTeacherAndDomainAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeacherDomainPricing?)null);

        var marketRepo = new Mock<IPricingMarketRepository>();
        marketRepo.Setup(r => r.GetByCodeAsync(MarketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMarket());

        var engine = new PricingEngine(
            priceRepo.Object, teacherRepo.Object, domainPricingRepo.Object, marketRepo.Object);

        var result = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(0m, result.TeacherSharePct);
        Assert.Equal(100m, result.TotalPrice);
    }
}
