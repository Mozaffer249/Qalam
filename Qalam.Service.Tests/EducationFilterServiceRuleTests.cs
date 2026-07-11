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

    var service = CreateService(
      domainRepo: domainRepo.Object,
      curriculumRepo: curriculumRepo.Object);

    var response = await service.GetFilterOptionsAsync(new FilterStateDto { DomainId = domainId });

    Assert.Equal("Curriculum", response.NextStep);
    Assert.Single(response.Options);
    Assert.True(response.Rule.HasCurriculum);
  }

  [Fact]
  public async Task GetFilterOptionsAsync_QuranDomain_ReturnsQuranContentTypeAsNextStep()
  {
    const int domainId = 2;
    const int subjectId = 100;
    var rule = CreateQuranRule(domainId);
    var service = CreateQuranService(domainId, rule, subjectId);

    var response = await service.GetFilterOptionsAsync(new FilterStateDto { DomainId = domainId });

    Assert.Equal("QuranContentType", response.NextStep);
    Assert.Equal(2, response.Options.Count);
    Assert.Equal(subjectId, response.CurrentState.SubjectId);
    Assert.NotNull(response.SelectedSubject);
    Assert.True(response.Rule.HasLessons);
  }

  [Fact]
  public async Task GetFilterOptionsAsync_QuranWithContentType_ReturnsQuranLevelAsNextStep()
  {
    const int domainId = 2;
    const int subjectId = 100;
    var rule = CreateQuranRule(domainId);
    var service = CreateQuranService(domainId, rule, subjectId);

    var response = await service.GetFilterOptionsAsync(new FilterStateDto
    {
      DomainId = domainId,
      SubjectId = subjectId,
      QuranContentTypeId = 1,
    });

    Assert.Equal("QuranLevel", response.NextStep);
    Assert.Equal(2, response.Options.Count);
  }

  [Fact]
  public async Task GetFilterOptionsAsync_QuranWithTypeAndLevel_ReturnsUnitAsNextStep()
  {
    const int domainId = 2;
    const int subjectId = 100;
    var rule = CreateQuranRule(domainId);
    var contentUnitRepo = new Mock<IContentUnitRepository>();
    contentUnitRepo
      .Setup(r => r.GetContentUnitsAsOptionsAsync(subjectId, "QuranPart", 1, 20, null))
      .ReturnsAsync((
        new List<FilterOptionDto> { new() { Id = 50, NameEn = "Juz 1", NameAr = "الجزء 1" } },
        1));

    var service = CreateQuranService(domainId, rule, subjectId, contentUnitRepo: contentUnitRepo.Object);

    var response = await service.GetFilterOptionsAsync(new FilterStateDto
    {
      DomainId = domainId,
      SubjectId = subjectId,
      QuranContentTypeId = 1,
      QuranLevelId = 10,
    });

    Assert.Equal("Unit", response.NextStep);
    Assert.Single(response.Unit!);
    Assert.Equal(50, response.Unit![0].Id);
  }

  [Fact]
  public async Task GetFilterOptionsAsync_QuranWithUnit_ReturnsLessonsScopedByTypeAndLevel()
  {
    const int domainId = 2;
    const int subjectId = 100;
    const int unitId = 50;
    var rule = CreateQuranRule(domainId);

    var contentUnitRepo = new Mock<IContentUnitRepository>();
    contentUnitRepo
      .Setup(r => r.GetByIdAsync(unitId))
      .ReturnsAsync(new ContentUnit { Id = unitId, SubjectId = subjectId });

    var lessonRepo = new Mock<ILessonRepository>();
    lessonRepo
      .Setup(r => r.GetLessonsAsOptionsAsync(unitId, 1, 10))
      .ReturnsAsync([new FilterOptionDto { Id = 200, NameEn = "Ayah 1-5", NameAr = "آية 1-5" }]);

    var service = CreateQuranService(
      domainId,
      rule,
      subjectId,
      contentUnitRepo: contentUnitRepo.Object,
      lessonRepo: lessonRepo.Object);

    var response = await service.GetFilterOptionsAsync(new FilterStateDto
    {
      DomainId = domainId,
      SubjectId = subjectId,
      QuranContentTypeId = 1,
      QuranLevelId = 10,
      ContentUnitId = unitId,
    });

    Assert.Equal("Lesson", response.NextStep);
    Assert.Single(response.Options);
    lessonRepo.Verify(r => r.GetLessonsAsOptionsAsync(unitId, 1, 10), Times.Once);
  }

  [Fact]
  public async Task GetFilterOptionsAsync_QuranWithSkipLessons_ReturnsDone()
  {
    const int domainId = 2;
    const int subjectId = 100;
    const int unitId = 50;
    var rule = CreateQuranRule(domainId);

    var contentUnitRepo = new Mock<IContentUnitRepository>();
    contentUnitRepo
      .Setup(r => r.GetByIdAsync(unitId))
      .ReturnsAsync(new ContentUnit { Id = unitId, SubjectId = subjectId });

    var service = CreateQuranService(domainId, rule, subjectId, contentUnitRepo: contentUnitRepo.Object);

    var response = await service.GetFilterOptionsAsync(new FilterStateDto
    {
      DomainId = domainId,
      SubjectId = subjectId,
      QuranContentTypeId = 1,
      QuranLevelId = 10,
      ContentUnitId = unitId,
      SkipLessons = true,
    });

    Assert.Equal("Done", response.NextStep);
  }

  private static EducationRule CreateQuranRule(int domainId) => new()
  {
    DomainId = domainId,
    HasCurriculum = false,
    HasEducationLevel = false,
    HasGrade = false,
    HasAcademicTerm = false,
    HasContentUnits = true,
    HasLessons = true,
    RequiresQuranContentType = true,
    RequiresQuranLevel = true,
    RequiresUnitTypeSelection = true,
    MinSessions = 1,
    MaxSessions = 200,
    DefaultSessionDurationMinutes = 45,
  };

  private static EducationFilterService CreateQuranService(
    int domainId,
    EducationRule rule,
    int subjectId,
    IContentUnitRepository? contentUnitRepo = null,
    ILessonRepository? lessonRepo = null)
  {
    var domainRepo = new Mock<IEducationDomainRepository>();
    domainRepo.Setup(r => r.GetEducationRuleByDomainIdAsync(domainId)).ReturnsAsync(rule);
    domainRepo.Setup(r => r.GetByIdAsync(domainId)).ReturnsAsync(new EducationDomain
    {
      Id = domainId,
      Code = "quran",
      NameEn = "Quran",
      NameAr = "قرآن",
    });

    var subjectRepo = new Mock<ISubjectRepository>();
    subjectRepo
      .Setup(r => r.GetSubjectsAsOptionsAsync(domainId, null, null, null, null))
      .ReturnsAsync([new FilterOptionDto { Id = subjectId, NameEn = "Holy Quran", NameAr = "القرآن الكريم" }]);

    var quranContentTypeRepo = new Mock<IQuranContentTypeRepository>();
    quranContentTypeRepo
      .Setup(r => r.GetQuranContentTypesAsOptionsAsync())
      .ReturnsAsync([
        new FilterOptionDto { Id = 1, NameEn = "Memorization", NameAr = "حفظ" },
        new FilterOptionDto { Id = 2, NameEn = "Recitation", NameAr = "تلاوة" },
      ]);

    var quranLevelRepo = new Mock<IQuranLevelRepository>();
    quranLevelRepo
      .Setup(r => r.GetQuranLevelsAsOptionsAsync())
      .ReturnsAsync([
        new FilterOptionDto { Id = 10, NameEn = "Beginner", NameAr = "مبتدئ" },
        new FilterOptionDto { Id = 11, NameEn = "Advanced", NameAr = "متقدم" },
      ]);

    return CreateService(
      domainRepo: domainRepo.Object,
      subjectRepo: subjectRepo.Object,
      contentUnitRepo: contentUnitRepo ?? Mock.Of<IContentUnitRepository>(),
      lessonRepo: lessonRepo ?? Mock.Of<ILessonRepository>(),
      quranContentTypeRepo: quranContentTypeRepo.Object,
      quranLevelRepo: quranLevelRepo.Object);
  }

  private static EducationFilterService CreateService(
    IEducationDomainRepository? domainRepo = null,
    ICurriculumRepository? curriculumRepo = null,
    IEducationLevelRepository? levelRepo = null,
    IGradeRepository? gradeRepo = null,
    IAcademicTermRepository? termRepo = null,
    ISubjectRepository? subjectRepo = null,
    IContentUnitRepository? contentUnitRepo = null,
    ILessonRepository? lessonRepo = null,
    IQuranContentTypeRepository? quranContentTypeRepo = null,
    IQuranLevelRepository? quranLevelRepo = null)
  {
    return new EducationFilterService(
      domainRepo ?? Mock.Of<IEducationDomainRepository>(),
      curriculumRepo ?? Mock.Of<ICurriculumRepository>(),
      levelRepo ?? Mock.Of<IEducationLevelRepository>(),
      gradeRepo ?? Mock.Of<IGradeRepository>(),
      termRepo ?? Mock.Of<IAcademicTermRepository>(),
      subjectRepo ?? Mock.Of<ISubjectRepository>(),
      contentUnitRepo ?? Mock.Of<IContentUnitRepository>(),
      lessonRepo ?? Mock.Of<ILessonRepository>(),
      quranContentTypeRepo ?? Mock.Of<IQuranContentTypeRepository>(),
      quranLevelRepo ?? Mock.Of<IQuranLevelRepository>());
  }
}
