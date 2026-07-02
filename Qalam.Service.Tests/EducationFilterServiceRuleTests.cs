using Moq;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class EducationFilterServiceRuleTests
{
    [Fact]
    public async Task GetFilterOptionsAsync_SchoolLikeRule_ReturnsCurriculumAsNextStep()
    {
        const int domainId = 1;
        var rule = new EducationRule
        {
            DomainId = domainId,
            HasCurriculum = true,
            HasEducationLevel = true,
            HasGrade = true,
            HasAcademicTerm = true,
            HasContentUnits = true,
            HasLessons = true,
            MinSessions = 1,
            MaxSessions = 200,
            DefaultSessionDurationMinutes = 45,
        };

        var domainRepo = new Mock<IEducationDomainRepository>();
        domainRepo.Setup(r => r.GetEducationRuleByDomainIdAsync(domainId)).ReturnsAsync(rule);
        domainRepo.Setup(r => r.GetByIdAsync(domainId)).ReturnsAsync(new EducationDomain
        {
            Id = domainId,
            Code = "school",
            NameEn = "School",
            NameAr = "مدرسة",
        });

        var curriculumRepo = new Mock<ICurriculumRepository>();
        curriculumRepo
            .Setup(r => r.GetCurriculumsAsOptionsAsync(domainId))
            .ReturnsAsync([new FilterOptionDto { Id = 10, NameEn = "National", NameAr = "وطني" }]);

        var service = new EducationFilterService(
            domainRepo.Object,
            curriculumRepo.Object,
            Mock.Of<IEducationLevelRepository>(),
            Mock.Of<IGradeRepository>(),
            Mock.Of<IAcademicTermRepository>(),
            Mock.Of<ISubjectRepository>(),
            Mock.Of<IContentUnitRepository>(),
            Mock.Of<ILessonRepository>(),
            Mock.Of<IQuranContentTypeRepository>(),
            Mock.Of<IQuranLevelRepository>());

        var response = await service.GetFilterOptionsAsync(new FilterStateDto { DomainId = domainId });

        Assert.Equal("Curriculum", response.NextStep);
        Assert.Single(response.Options);
        Assert.True(response.Rule.HasCurriculum);
    }
}
