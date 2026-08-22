using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Infrastructure.Seeding;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PricingAdminServiceTests
{
    private static ApplicationDBContext CreateDb(string? databaseName = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EncryptionSettings:Key"] = "0123456789abcdef0123456789abcdef",
            })
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDBContext(options, config);
    }

    private static PricingAdminService CreateSut(
        out Mock<IDomainSessionPriceRepository> priceRepo,
        out Mock<IPricingMarketRepository> marketRepo,
        out Mock<IEducationDomainRepository> domainRepo,
        out Mock<ITeacherLevelRepository> levelRepo,
        out Mock<ITeacherRepository> teacherRepo,
        out Mock<ITeacherLevelUpgradeSuggestionRepository> suggestionRepo,
        out Mock<ITeacherDomainPricingRepository> domainPricingRepo,
        ApplicationDBContext? dbContext = null)
    {
        priceRepo = new Mock<IDomainSessionPriceRepository>();
        marketRepo = new Mock<IPricingMarketRepository>();
        domainRepo = new Mock<IEducationDomainRepository>();
        levelRepo = new Mock<ITeacherLevelRepository>();
        teacherRepo = new Mock<ITeacherRepository>();
        suggestionRepo = new Mock<ITeacherLevelUpgradeSuggestionRepository>();
        domainPricingRepo = new Mock<ITeacherDomainPricingRepository>();

        marketRepo
            .Setup(r => r.ExistsActiveAsync("sa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return new PricingAdminService(
            priceRepo.Object,
            marketRepo.Object,
            domainRepo.Object,
            levelRepo.Object,
            teacherRepo.Object,
            domainPricingRepo.Object,
            suggestionRepo.Object,
            new DomainRatePropagationService(priceRepo.Object, marketRepo.Object),
            dbContext ?? CreateDb());
    }

    [Fact]
    public async Task SetDomainSessionPriceAsync_DomainNotFound_ReturnsNull()
    {
        var service = CreateSut(out _, out _, out var domainRepo, out _, out _, out _, out _);
        domainRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((EducationDomain?)null);

        var result = await service.SetDomainSessionPriceAsync(new SetDomainSessionPriceDto
        {
            MarketCode = "sa",
            DomainId = 1,
            SessionTypeCode = "individual",
            PricePerHour = 100m
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task SetDomainSessionPriceAsync_InvalidSessionType_Throws()
    {
        var service = CreateSut(out _, out _, out var domainRepo, out _, out _, out _, out _);
        domainRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new EducationDomain
        {
            Id = 1,
            Code = "school",
            NameEn = "School",
            NameAr = "مدرسة"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetDomainSessionPriceAsync(new SetDomainSessionPriceDto
            {
                MarketCode = "sa",
                DomainId = 1,
                SessionTypeCode = "pair",
                PricePerHour = 100m
            }));

        Assert.Contains("individual' or 'group", ex.Message);
    }

    [Fact]
    public async Task SetDomainSessionPriceAsync_SamePrice_ReturnsCurrentWithoutAdding()
    {
        var current = new DomainSessionPrice
        {
            Id = 7,
            DomainId = 1,
            SessionTypeCode = "individual",
            PricePerHour = 100m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        };

        var service = CreateSut(out var priceRepo, out _, out var domainRepo, out _, out _, out _, out _);
        domainRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new EducationDomain
        {
            Id = 1,
            Code = "school",
            NameEn = "School",
            NameAr = "مدرسة"
        });
        priceRepo.Setup(r => r.GetCurrentRateAsync(1, "individual", "sa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);

        var result = await service.SetDomainSessionPriceAsync(new SetDomainSessionPriceDto
        {
            MarketCode = "sa",
            DomainId = 1,
            SessionTypeCode = "Individual",
            PricePerHour = 100m
        });

        Assert.NotNull(result);
        Assert.Equal(7, result!.Id);
        Assert.Equal(100m, result.PricePerHour);
        priceRepo.Verify(r => r.AddAsync(It.IsAny<DomainSessionPrice>()), Times.Never);
        priceRepo.Verify(r => r.CloseCurrentRateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetDomainSessionPriceAsync_NonBaseMarket_Throws()
    {
        var service = CreateSut(out _, out _, out var domainRepo, out _, out _, out _, out _);
        domainRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new EducationDomain { Id = 1, Code = "school", NameEn = "School", NameAr = "مدرسة" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetDomainSessionPriceAsync(new SetDomainSessionPriceDto
            {
                MarketCode = "ae",
                DomainId = 1,
                SessionTypeCode = "individual",
                PricePerHour = 100m
            }));

        Assert.Contains("base market", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetDomainSessionPriceAsync_NewPrice_ClosesCurrentAndPropagates()
    {
        var addedRows = new List<DomainSessionPrice>();
        var service = CreateSut(out var priceRepo, out var marketRepo, out var domainRepo, out _, out _, out _, out _);
        domainRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new EducationDomain
        {
            Id = 1,
            Code = "school",
            NameEn = "School",
            NameAr = "مدرسة"
        });
        marketRepo.Setup(r => r.ListActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PricingMarket { Code = "sa", Currency = "SAR", ExchangeRateFromBase = 1m, IsActive = true },
                new PricingMarket { Code = "ae", Currency = "AED", ExchangeRateFromBase = 1m, IsActive = true },
            ]);
        marketRepo.Setup(r => r.GetByCodeAsync("sa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingMarket { Code = "sa", Currency = "SAR", ExchangeRateFromBase = 1m });
        priceRepo.Setup(r => r.GetCurrentRateAsync(1, "group", "sa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DomainSessionPrice
            {
                Id = 3,
                DomainId = 1,
                SessionTypeCode = "group",
                PricePerHour = 75m
            });
        priceRepo.Setup(r => r.GetCurrentRateAsync(1, "group", "ae", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainSessionPrice?)null);
        priceRepo.Setup(r => r.AddAsync(It.IsAny<DomainSessionPrice>()))
            .Callback<DomainSessionPrice>(p => addedRows.Add(p))
            .ReturnsAsync((DomainSessionPrice p) => p);

        var result = await service.SetDomainSessionPriceAsync(new SetDomainSessionPriceDto
        {
            DomainId = 1,
            SessionTypeCode = "group",
            PricePerHour = 90m
        });

        Assert.NotNull(result);
        Assert.Contains(addedRows, r => r.MarketCode == "sa" && r.PricePerHour == 90m);
        Assert.Contains(addedRows, r => r.MarketCode == "ae" && r.PricePerHour == 90m);
        priceRepo.Verify(r => r.CloseCurrentRateAsync(1, "group", "sa", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        priceRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetTeacherLevelAsync_InactiveLevel_Throws()
    {
        var service = CreateSut(out _, out _, out _, out var levelRepo, out var teacherRepo, out _, out _);
        teacherRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Teacher { Id = 5 });
        levelRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new TeacherLevel
        {
            Id = 2,
            Code = "intermediate",
            IsActive = false,
            TeacherSharePct = 70m
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetTeacherLevelAsync(5, new SetTeacherLevelDto { DomainId = 1, TeacherLevelId = 2 }));

        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApproveLevelUpgradeSuggestionAsync_Pending_UpdatesTeacherAndSuggestion()
    {
        Teacher? updatedTeacher = null;
        TeacherLevelUpgradeSuggestion? updatedSuggestion = null;

        var suggestion = new TeacherLevelUpgradeSuggestion
        {
            Id = 11,
            TeacherId = 5,
            DomainId = 3,
            CurrentLevelId = 1,
            SuggestedLevelId = 2,
            Status = TeacherLevelUpgradeSuggestionStatus.Pending,
            CurrentLevel = new TeacherLevel { Id = 1, Code = "starter" },
            SuggestedLevel = new TeacherLevel { Id = 2, Code = "intermediate" },
            Teacher = new Teacher { Id = 5 }
        };

        var service = CreateSut(out _, out _, out _, out _, out var teacherRepo, out var suggestionRepo, out var domainPricingRepo);
        suggestionRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(suggestion);
        teacherRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Teacher { Id = 5, TeacherLevelId = 1 });
        teacherRepo.Setup(r => r.UpdateAsync(It.IsAny<Teacher>()))
            .Callback<Teacher>(t => updatedTeacher = t)
            .Returns(Task.CompletedTask);
        suggestionRepo.Setup(r => r.UpdateAsync(It.IsAny<TeacherLevelUpgradeSuggestion>()))
            .Callback<TeacherLevelUpgradeSuggestion>(s => updatedSuggestion = s)
            .Returns(Task.CompletedTask);
        domainPricingRepo
            .Setup(r => r.GetOrCreateAsync(5, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherDomainPricing { TeacherId = 5, DomainId = 3, TeacherLevelId = 1 });
        domainPricingRepo.Setup(r => r.UpdateAsync(It.IsAny<TeacherDomainPricing>())).Returns(Task.CompletedTask);

        var success = await service.ApproveLevelUpgradeSuggestionAsync(11, "Looks good");

        Assert.True(success);
        Assert.Equal(2, updatedTeacher!.TeacherLevelId);
        Assert.Equal(TeacherLevelUpgradeSuggestionStatus.Approved, updatedSuggestion!.Status);
        Assert.Equal("Looks good", updatedSuggestion.ReviewNotes);
        suggestionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RejectLevelUpgradeSuggestionAsync_NotPending_Throws()
    {
        var service = CreateSut(out _, out _, out _, out _, out _, out var suggestionRepo, out _);
        suggestionRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new TeacherLevelUpgradeSuggestion
        {
            Id = 11,
            Status = TeacherLevelUpgradeSuggestionStatus.Approved
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RejectLevelUpgradeSuggestionAsync(11, "Too late"));

        Assert.Contains("not pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListLevelUpgradeSuggestionsAsync_InvalidStatus_Throws()
    {
        var service = CreateSut(out _, out _, out _, out _, out _, out _, out _);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ListLevelUpgradeSuggestionsAsync("unknown"));

        Assert.Contains("Invalid status filter", ex.Message);
    }

    [Fact]
    public async Task BackfillStarterTeacherLevelsAsync_DelegatesToRepository()
    {
        var service = CreateSut(out _, out _, out _, out _, out var teacherRepo, out _, out _);
        teacherRepo
            .Setup(r => r.BackfillStarterLevelForTeachersWithoutLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2500);

        var result = await service.BackfillStarterTeacherLevelsAsync();

        Assert.Equal(2500, result.UpdatedCount);
    }

    [Fact]
    public async Task CreatePricingMarketAsync_DuplicateCode_Throws()
    {
        var service = CreateSut(out _, out var marketRepo, out _, out _, out _, out _, out _);
        marketRepo.Setup(r => r.ExistsAsync("ma", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePricingMarketAsync(new CreatePricingMarketDto
            {
                Code = "ma",
                NameEn = "Morocco",
                NameAr = "المغرب",
                Currency = "MAD"
            }));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdatePricingMarketAsync_DeactivateDefault_Throws()
    {
        var market = new PricingMarket
        {
            Code = "sa",
            NameEn = "Saudi Arabia",
            NameAr = "السعودية",
            Currency = "SAR",
            IsActive = true,
            IsDefault = true
        };

        var service = CreateSut(out _, out var marketRepo, out _, out _, out _, out _, out _);
        marketRepo.Setup(r => r.GetByCodeTrackedAsync("sa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(market);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdatePricingMarketAsync("sa", new UpdatePricingMarketDto
            {
                NameEn = market.NameEn,
                NameAr = market.NameAr,
                Currency = market.Currency,
                IsActive = false
            }));

        Assert.Contains("default market", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetDefaultPricingMarketAsync_InactiveMarket_Throws()
    {
        var service = CreateSut(out _, out var marketRepo, out _, out _, out _, out _, out _);
        marketRepo.Setup(r => r.GetByCodeTrackedAsync("eg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingMarket
            {
                Code = "eg",
                NameEn = "Egypt",
                NameAr = "مصر",
                Currency = "EGP",
                IsActive = false
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetDefaultPricingMarketAsync("eg"));

        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePricingMarketAsync_SeedsPlaceholderRatesForAllDomains()
    {
        await using var db = CreateDb();
        var now = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        db.EducationDomains.AddRange(
            new EducationDomain { Id = 1, Code = "school", NameEn = "School", NameAr = "مدرسة", IsActive = true, CreatedAt = now },
            new EducationDomain { Id = 2, Code = "quran", NameEn = "Quran", NameAr = "قرآن", IsActive = true, CreatedAt = now });
        await db.SaveChangesAsync();

        var marketRepo = new PricingMarketRepository(db);
        var priceRepo = new DomainSessionPriceRepository(db);
        var service = new PricingAdminService(
            priceRepo,
            marketRepo,
            new Mock<IEducationDomainRepository>().Object,
            new Mock<ITeacherLevelRepository>().Object,
            new Mock<ITeacherRepository>().Object,
            new Mock<ITeacherDomainPricingRepository>().Object,
            new Mock<ITeacherLevelUpgradeSuggestionRepository>().Object,
            new DomainRatePropagationService(priceRepo, marketRepo),
            db);

        var result = await service.CreatePricingMarketAsync(new CreatePricingMarketDto
        {
            Code = "ma",
            NameEn = "Morocco",
            NameAr = "المغرب",
            Currency = "MAD"
        });

        Assert.Equal("ma", result.Code);
        Assert.True(result.IsActive);

        var rates = db.DomainSessionPrices.Where(p => p.MarketCode == "ma").ToList();
        Assert.Equal(4, rates.Count);
        Assert.Contains(rates, r => r.DomainId == 1 && r.SessionTypeCode == PricingDefaults.SessionTypeIndividual);
        Assert.Contains(rates, r => r.DomainId == 1 && r.SessionTypeCode == PricingDefaults.SessionTypeGroup);
    }

    [Fact]
    public async Task SetDefaultPricingMarketAsync_SetsDefaultAndClearsOthers()
    {
        var sa = new PricingMarket
        {
            Code = "sa",
            NameEn = "SA",
            NameAr = "SA",
            Currency = "SAR",
            IsActive = true,
            IsDefault = true,
        };
        var eg = new PricingMarket
        {
            Code = "eg",
            NameEn = "EG",
            NameAr = "EG",
            Currency = "EGP",
            IsActive = true,
            IsDefault = false,
        };

        PricingMarket? updated = null;
        var service = CreateSut(out _, out var marketRepo, out _, out _, out _, out _, out _);
        marketRepo.Setup(r => r.GetByCodeTrackedAsync("eg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(eg);
        marketRepo.Setup(r => r.ClearDefaultFlagAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                sa.IsDefault = false;
                eg.IsDefault = true;
            })
            .Returns(Task.CompletedTask);
        marketRepo.Setup(r => r.UpdateAsync(It.IsAny<PricingMarket>()))
            .Callback<PricingMarket>(m => updated = m)
            .Returns(Task.CompletedTask);

        var result = await service.SetDefaultPricingMarketAsync("eg");

        Assert.NotNull(result);
        Assert.True(result!.IsDefault);
        Assert.Equal("eg", updated!.Code);
        marketRepo.Verify(r => r.ClearDefaultFlagAsync(It.IsAny<CancellationToken>()), Times.Once);
        marketRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePricingMarketAsync_PersistsNamesAndCurrency()
    {
        PricingMarket? updated = null;
        var market = new PricingMarket
        {
            Code = "ae",
            NameEn = "UAE",
            NameAr = "الإمارات",
            Currency = "AED",
            IsActive = true,
            IsDefault = false
        };

        var service = CreateSut(out _, out var marketRepo, out _, out _, out _, out _, out _);
        marketRepo.Setup(r => r.GetByCodeTrackedAsync("ae", It.IsAny<CancellationToken>()))
            .ReturnsAsync(market);
        marketRepo.Setup(r => r.UpdateAsync(It.IsAny<PricingMarket>()))
            .Callback<PricingMarket>(m => updated = m)
            .Returns(Task.CompletedTask);

        var result = await service.UpdatePricingMarketAsync("ae", new UpdatePricingMarketDto
        {
            NameEn = "United Arab Emirates",
            NameAr = "الإمارات العربية",
            Currency = "AED",
            IsActive = true
        });

        Assert.NotNull(result);
        Assert.Equal("United Arab Emirates", updated!.NameEn);
        marketRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePricingExchangeRateAsync_BaseMarket_Throws()
    {
        var service = CreateSut(out _, out _, out _, out _, out _, out _, out _);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdatePricingExchangeRateAsync("sa", new UpdatePricingExchangeRateDto { ExchangeRateFromBase = 2m }));

        Assert.Contains("base market", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdatePricingExchangeRateAsync_RecalculatesDerivedRates()
    {
        await using var db = CreateDb();
        var now = DateTime.UtcNow;
        db.PricingMarkets.AddRange(
            new PricingMarket { Code = "sa", NameEn = "SA", NameAr = "SA", Currency = "SAR", IsActive = true, IsDefault = true, ExchangeRateFromBase = 1m, CreatedAt = now },
            new PricingMarket { Code = "ae", NameEn = "AE", NameAr = "AE", Currency = "AED", IsActive = true, IsDefault = false, ExchangeRateFromBase = 1m, CreatedAt = now });
        db.EducationDomains.Add(new EducationDomain { Id = 1, Code = "school", NameEn = "School", NameAr = "مدرسة", IsActive = true, CreatedAt = now });
        db.DomainSessionPrices.AddRange(
            new DomainSessionPrice { MarketCode = "sa", DomainId = 1, SessionTypeCode = "individual", PricePerHour = 100m, EffectiveFrom = now, IsActive = true, CreatedAt = now },
            new DomainSessionPrice { MarketCode = "ae", DomainId = 1, SessionTypeCode = "individual", PricePerHour = 100m, EffectiveFrom = now, IsActive = true, CreatedAt = now });
        await db.SaveChangesAsync();

        var marketRepo = new PricingMarketRepository(db);
        var priceRepo = new DomainSessionPriceRepository(db);
        var service = new PricingAdminService(
            priceRepo,
            marketRepo,
            new Mock<IEducationDomainRepository>().Object,
            new Mock<ITeacherLevelRepository>().Object,
            new Mock<ITeacherRepository>().Object,
            new Mock<ITeacherDomainPricingRepository>().Object,
            new Mock<ITeacherLevelUpgradeSuggestionRepository>().Object,
            new DomainRatePropagationService(priceRepo, marketRepo),
            db);

        await service.UpdatePricingExchangeRateAsync("ae", new UpdatePricingExchangeRateDto { ExchangeRateFromBase = 2m });

        var aeRate = db.DomainSessionPrices
            .Where(p => p.MarketCode == "ae" && p.DomainId == 1 && p.SessionTypeCode == "individual" && p.EffectiveTo == null)
            .Single();
        Assert.Equal(200m, aeRate.PricePerHour);
    }

    [Fact]
    public async Task CreateTeacherLevelTierAsync_AppendsAboveSeededLevels()
    {
        await using var db = CreateDb();
        var now = DateTime.UtcNow;
        db.TeacherLevels.AddRange(PricingDefaults.CreateTeacherLevels(now));
        await db.SaveChangesAsync();

        var levelRepo = new TeacherLevelRepository(db);
        var service = new PricingAdminService(
            new Mock<IDomainSessionPriceRepository>().Object,
            new Mock<IPricingMarketRepository>().Object,
            new Mock<IEducationDomainRepository>().Object,
            levelRepo,
            new Mock<ITeacherRepository>().Object,
            new Mock<ITeacherDomainPricingRepository>().Object,
            new Mock<ITeacherLevelUpgradeSuggestionRepository>().Object,
            new Mock<IDomainRatePropagationService>().Object,
            db);

        var created = await service.CreateTeacherLevelTierAsync(new CreateTeacherLevelTierDto
        {
            Code = "expert",
            NameEn = "Expert",
            NameAr = "خبير",
            TeacherSharePct = 85m,
            IsActive = true
        });

        Assert.Equal("expert", created.Code);
        Assert.Equal(85m, created.TeacherSharePct);
        Assert.Equal(4, created.OrderIndex);

        var ordered = await levelRepo.ListOrderedAsync();
        Assert.Equal(4, ordered.Count);
        Assert.Equal("starter", (await levelRepo.GetStarterLevelAsync())!.Code);
    }
}

public class PricingDefaultsTests
{
    [Fact]
    public void CreateTeacherLevels_SeedsThreeOrderedTiers()
    {
        var levels = PricingDefaults.CreateTeacherLevels(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3, levels.Count);
        Assert.Equal(["starter", "intermediate", "advanced"], levels.Select(l => l.Code).ToArray());
        Assert.Equal([60m, 70m, 80m], levels.Select(l => l.TeacherSharePct).ToArray());
    }

    [Theory]
    [InlineData("school", 100, 75)]
    [InlineData("quran", 80, 60)]
    [InlineData("university", 150, 120)]
    [InlineData("soft-skills", 90, 70)]
    [InlineData("skills", 100, 75)]
    public void GetDomainRates_ReturnsExpectedValues(string domainCode, decimal individual, decimal group)
    {
        var rates = PricingDefaults.GetDomainRates(domainCode);

        Assert.Equal(individual, rates.Individual);
        Assert.Equal(group, rates.Group);
    }

    [Theory]
    [InlineData(100, 1, 100)]
    [InlineData(100, 0.08, 8)]
    [InlineData(100, 8, 800)]
    public void DeriveLocalPrice_RoundsAwayFromZero(decimal basePrice, decimal rate, decimal expected)
    {
        Assert.Equal(expected, PricingExchangeRateHelper.DeriveLocalPrice(basePrice, rate));
    }
}
