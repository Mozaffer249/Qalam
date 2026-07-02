using Moq;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class EducationDomainServiceTests
{
    [Fact]
    public async Task CreateDomainAsync_InsertsDomainWithEducationRule()
    {
        EducationDomain? captured = null;
        var repo = new Mock<IEducationDomainRepository>();
        repo.Setup(r => r.IsDomainCodeUniqueAsync("school", null)).ReturnsAsync(true);
        repo.Setup(r => r.AddAsync(It.IsAny<EducationDomain>()))
            .Callback<EducationDomain>(d => captured = d)
            .ReturnsAsync((EducationDomain d) =>
            {
                d.Id = 42;
                d.EducationRule!.DomainId = 42;
                return d;
            });

        var service = new EducationDomainService(repo.Object);
        var rule = new EducationRuleDto
        {
            HasCurriculum = true,
            HasEducationLevel = true,
            HasGrade = true,
            HasAcademicTerm = true,
            HasContentUnits = true,
            HasLessons = true,
            MinSessions = 1,
            MaxSessions = 200,
            DefaultSessionDurationMinutes = 45,
            MinGroupSize = 1,
            MaxGroupSize = 30,
        };

        var result = await service.CreateDomainAsync(new EducationDomain
        {
            NameAr = "مدرسة",
            NameEn = "School",
            Code = "school",
            IsActive = true,
        }, rule);

        Assert.Equal(42, result.Id);
        Assert.NotNull(captured?.EducationRule);
        Assert.True(captured.EducationRule.HasCurriculum);
        Assert.Equal(200, captured.EducationRule.MaxSessions);
        repo.Verify(r => r.AddAsync(It.IsAny<EducationDomain>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDomainAsync_UpsertsEducationRule()
    {
        var existing = new EducationDomain
        {
            Id = 5,
            NameAr = "قديم",
            NameEn = "Old",
            Code = "school",
            IsActive = true,
            EducationRule = new EducationRule
            {
                Id = 99,
                DomainId = 5,
                HasCurriculum = false,
                MinSessions = 1,
                MaxSessions = 50,
                DefaultSessionDurationMinutes = 60,
            }
        };

        var repo = new Mock<IEducationDomainRepository>();
        repo.Setup(r => r.GetDomainWithDetailsAsync(5)).ReturnsAsync(existing);
        repo.Setup(r => r.IsDomainCodeUniqueAsync("school", 5)).ReturnsAsync(true);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new EducationDomainService(repo.Object);
        var updatedRule = new EducationRuleDto
        {
            HasCurriculum = true,
            HasEducationLevel = true,
            MinSessions = 2,
            MaxSessions = 100,
            DefaultSessionDurationMinutes = 45,
        };

        var result = await service.UpdateDomainAsync(new EducationDomain
        {
            Id = 5,
            NameAr = "جديد",
            NameEn = "New",
            Code = "school",
            IsActive = false,
        }, updatedRule);

        Assert.Equal("New", result.NameEn);
        Assert.False(result.IsActive);
        Assert.NotNull(result.EducationRule);
        Assert.True(result.EducationRule.HasCurriculum);
        Assert.True(result.EducationRule.HasEducationLevel);
        Assert.Equal(2, result.EducationRule.MinSessions);
        Assert.Equal(100, result.EducationRule.MaxSessions);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
