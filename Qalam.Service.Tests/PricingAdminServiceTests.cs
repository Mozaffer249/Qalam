using Moq;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PricingAdminServiceTests
{
    private static PricingAdminService CreateSut(
        out Mock<IDomainSessionPriceRepository> priceRepo,
        out Mock<IEducationDomainRepository> domainRepo,
        out Mock<ITeacherLevelRepository> levelRepo,
        out Mock<ITeacherRepository> teacherRepo,
        out Mock<ITeacherLevelUpgradeSuggestionRepository> suggestionRepo)
    {
        priceRepo = new Mock<IDomainSessionPriceRepository>();
        domainRepo = new Mock<IEducationDomainRepository>();
        levelRepo = new Mock<ITeacherLevelRepository>();
        teacherRepo = new Mock<ITeacherRepository>();
        suggestionRepo = new Mock<ITeacherLevelUpgradeSuggestionRepository>();

        return new PricingAdminService(
            priceRepo.Object,
            domainRepo.Object,
            levelRepo.Object,
            teacherRepo.Object,
            suggestionRepo.Object);
    }

    [Fact]
    public async Task SetDomainSessionPriceAsync_DomainNotFound_ReturnsNull()
    {
        var service = CreateSut(out _, out var domainRepo, out _, out _, out _);
        domainRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((EducationDomain?)null);

        var result = await service.SetDomainSessionPriceAsync(new SetDomainSessionPriceDto
        {
            DomainId = 1,
            SessionTypeCode = "individual",
            PricePerHour = 100m
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task SetDomainSessionPriceAsync_InvalidSessionType_Throws()
    {
        var service = CreateSut(out _, out var domainRepo, out _, out _, out _);
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

        var service = CreateSut(out var priceRepo, out var domainRepo, out _, out _, out _);
        domainRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new EducationDomain
        {
            Id = 1,
            Code = "school",
            NameEn = "School",
            NameAr = "مدرسة"
        });
        priceRepo.Setup(r => r.GetCurrentRateAsync(1, "individual", It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);

        var result = await service.SetDomainSessionPriceAsync(new SetDomainSessionPriceDto
        {
            DomainId = 1,
            SessionTypeCode = "Individual",
            PricePerHour = 100m
        });

        Assert.NotNull(result);
        Assert.Equal(7, result!.Id);
        Assert.Equal(100m, result.PricePerHour);
        priceRepo.Verify(r => r.AddAsync(It.IsAny<DomainSessionPrice>()), Times.Never);
        priceRepo.Verify(r => r.CloseCurrentRateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetDomainSessionPriceAsync_NewPrice_ClosesCurrentAndAddsRow()
    {
        DomainSessionPrice? added = null;
        var service = CreateSut(out var priceRepo, out var domainRepo, out _, out _, out _);
        domainRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new EducationDomain
        {
            Id = 1,
            Code = "school",
            NameEn = "School",
            NameAr = "مدرسة"
        });
        priceRepo.Setup(r => r.GetCurrentRateAsync(1, "group", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DomainSessionPrice
            {
                Id = 3,
                DomainId = 1,
                SessionTypeCode = "group",
                PricePerHour = 75m
            });
        priceRepo.Setup(r => r.AddAsync(It.IsAny<DomainSessionPrice>()))
            .Callback<DomainSessionPrice>(p => added = p)
            .ReturnsAsync((DomainSessionPrice p) => p);

        var result = await service.SetDomainSessionPriceAsync(new SetDomainSessionPriceDto
        {
            DomainId = 1,
            SessionTypeCode = "group",
            PricePerHour = 90m
        });

        Assert.NotNull(result);
        Assert.NotNull(added);
        Assert.Equal("group", added!.SessionTypeCode);
        Assert.Equal(90m, added.PricePerHour);
        priceRepo.Verify(r => r.CloseCurrentRateAsync(1, "group", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        priceRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetTeacherLevelAsync_InactiveLevel_Throws()
    {
        var service = CreateSut(out _, out _, out var levelRepo, out var teacherRepo, out _);
        teacherRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Teacher { Id = 5 });
        levelRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new TeacherLevel
        {
            Id = 2,
            Code = "intermediate",
            IsActive = false,
            TeacherSharePct = 70m
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetTeacherLevelAsync(5, new SetTeacherLevelDto { TeacherLevelId = 2 }));

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
            CurrentLevelId = 1,
            SuggestedLevelId = 2,
            Status = TeacherLevelUpgradeSuggestionStatus.Pending,
            CurrentLevel = new TeacherLevel { Id = 1, Code = "starter" },
            SuggestedLevel = new TeacherLevel { Id = 2, Code = "intermediate" },
            Teacher = new Teacher { Id = 5 }
        };

        var service = CreateSut(out _, out _, out _, out var teacherRepo, out var suggestionRepo);
        suggestionRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(suggestion);
        teacherRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Teacher { Id = 5, TeacherLevelId = 1 });
        teacherRepo.Setup(r => r.UpdateAsync(It.IsAny<Teacher>()))
            .Callback<Teacher>(t => updatedTeacher = t)
            .Returns(Task.CompletedTask);
        suggestionRepo.Setup(r => r.UpdateAsync(It.IsAny<TeacherLevelUpgradeSuggestion>()))
            .Callback<TeacherLevelUpgradeSuggestion>(s => updatedSuggestion = s)
            .Returns(Task.CompletedTask);

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
        var service = CreateSut(out _, out _, out _, out _, out var suggestionRepo);
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
        var service = CreateSut(out _, out _, out _, out _, out _);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ListLevelUpgradeSuggestionsAsync("unknown"));

        Assert.Contains("Invalid status filter", ex.Message);
    }

    [Fact]
    public async Task BackfillStarterTeacherLevelsAsync_DelegatesToRepository()
    {
        var service = CreateSut(out _, out _, out _, out var teacherRepo, out _);
        teacherRepo
            .Setup(r => r.BackfillStarterLevelForTeachersWithoutLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2500);

        var result = await service.BackfillStarterTeacherLevelsAsync();

        Assert.Equal(2500, result.UpdatedCount);
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
}
