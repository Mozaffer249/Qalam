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

    private static DomainSessionPrice CreateRate(decimal pricePerHour = 100m, int id = 5) =>
        new()
        {
            Id = id,
            MarketCode = MarketCode,
            DomainId = DomainId,
            SessionTypeCode = SessionType,
            PricePerHour = pricePerHour,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        };

    private static PricingMarket CreateMarket(string code = MarketCode, string currency = Currency) =>
        new()
        {
            Code = code,
            Currency = currency,
            NameEn = "Saudi Arabia",
            NameAr = "السعودية",
            IsActive = true,
            IsDefault = code == MarketCode
        };

    private static Teacher CreateTeacher(
        decimal? customShare = null,
        decimal levelShare = 60m,
        int levelId = 1)
    {
        var level = new TeacherLevel
        {
            Id = levelId,
            Code = "starter",
            NameAr = "مبتدئ",
            NameEn = "Starter",
            OrderIndex = 1,
            TeacherSharePct = levelShare,
            IsActive = true
        };

        return new Teacher
        {
            Id = TeacherId,
            TeacherLevelId = levelId,
            TeacherLevel = level,
            CustomTeacherSharePct = customShare,
            HasCompletedInterviewSession = true
        };
    }

    private static (PricingEngine Engine, Mock<IDomainSessionPriceRepository> PriceRepo, Mock<ITeacherRepository> TeacherRepo)
        CreateSut(Teacher? teacher = null, DomainSessionPrice? rate = null, bool missingRate = false, PricingMarket? market = null)
    {
        var priceRepo = new Mock<IDomainSessionPriceRepository>();
        priceRepo
            .Setup(r => r.GetEffectiveRateAsync(DomainId, SessionType, MarketCode, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(missingRate ? null : (rate ?? CreateRate()));

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo
            .Setup(r => r.GetByIdWithLevelAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacher ?? CreateTeacher());

        var marketRepo = new Mock<IPricingMarketRepository>();
        marketRepo
            .Setup(r => r.GetByCodeAsync(MarketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(market ?? CreateMarket());

        var engine = new PricingEngine(priceRepo.Object, teacherRepo.Object, marketRepo.Object);
        return (engine, priceRepo, teacherRepo);
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
        var (engine, _, _) = CreateSut();

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
        teacherRepo
            .Setup(r => r.GetByIdWithLevelAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTeacher());

        var marketRepo = new Mock<IPricingMarketRepository>();
        marketRepo.Setup(r => r.GetByCodeAsync("eg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMarket("eg", "EGP"));

        var engine = new PricingEngine(priceRepo.Object, teacherRepo.Object, marketRepo.Object);

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
        var (engine, _, _) = CreateSut(rate: CreateRate(100m));

        var estimate = await engine.EstimateAsync(CreateRequest(45));

        Assert.Equal(75m, estimate.TotalPrice);
        Assert.Equal(45m, estimate.TeacherEarnings);
        Assert.Equal(30m, estimate.PlatformShare);
    }

    [Fact]
    public async Task EstimateAsync_UsesCustomTeacherShareOverride_WhenSet()
    {
        var (engine, _, _) = CreateSut(teacher: CreateTeacher(customShare: 85m, levelShare: 60m));

        var estimate = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(85m, estimate.TeacherSharePct);
        Assert.Equal(85m, estimate.TeacherEarnings);
        Assert.Equal(15m, estimate.PlatformShare);
    }

    [Fact]
    public async Task EstimateAsync_UsesTeacherLevelShare_WhenNoOverride()
    {
        var (engine, _, _) = CreateSut(teacher: CreateTeacher(levelShare: 80m, levelId: 3));

        var estimate = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(80m, estimate.TeacherSharePct);
        Assert.Equal(80m, estimate.TeacherEarnings);
        Assert.Equal(20m, estimate.PlatformShare);
        Assert.Equal(3, estimate.TeacherLevelId);
    }

    [Fact]
    public async Task EstimateAsync_ZeroMinutes_Throws()
    {
        var (engine, _, _) = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine.EstimateAsync(CreateRequest(0)));
    }

    [Fact]
    public async Task EstimateAsync_NoRate_Throws()
    {
        var (engine, _, _) = CreateSut(missingRate: true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine.EstimateAsync(CreateRequest(60)));

        Assert.Contains("No active pricing rule", ex.Message);
    }

    [Fact]
    public async Task EstimateAsync_TeacherNotFound_Throws()
    {
        var (engine, _, teacherRepo) = CreateSut();
        teacherRepo
            .Setup(r => r.GetByIdWithLevelAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Teacher?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine.EstimateAsync(CreateRequest(60)));

        Assert.Contains($"Teacher {TeacherId} not found", ex.Message);
    }

    [Fact]
    public async Task EstimateAsync_TeacherNoLevel_UsesZeroShare()
    {
        var teacher = CreateTeacher();
        teacher.TeacherLevel = null;
        var (engine, _, _) = CreateSut(teacher: teacher);

        var result = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(0m, result.TeacherSharePct);
        Assert.Equal(0m, result.TeacherEarnings);
        Assert.Equal(100m, result.TotalPrice);
    }

    [Fact]
    public async Task ResolvePricePerHourAsync_ReturnsEffectiveRate()
    {
        var (engine, _, _) = CreateSut(rate: CreateRate(150m));

        var price = await engine.ResolvePricePerHourAsync(DomainId, SessionType, MarketCode);

        Assert.Equal(150m, price);
    }

    [Fact]
    public async Task CreateSnapshotAsync_CopiesEstimateFields()
    {
        var (engine, _, _) = CreateSut();

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
        var teacher = CreateTeacher();
        teacher.HasCompletedInterviewSession = false;
        teacher.TeacherLevelId = null;
        teacher.TeacherLevel = null;

        var (engine, _, _) = CreateSut(teacher);

        var result = await engine.EstimateAsync(CreateRequest(60));

        Assert.Equal(0m, result.TeacherSharePct);
        Assert.Equal(0m, result.TeacherEarnings);
        Assert.Equal(100m, result.TotalPrice);
        Assert.Equal(100m, result.PlatformShare);
    }
}
