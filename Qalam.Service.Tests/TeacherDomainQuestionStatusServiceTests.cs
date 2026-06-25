using Moq;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class TeacherDomainQuestionStatusServiceTests
{
    private const int TeacherId = 7;
    private const int DomainId = 1;

    [Fact]
    public async Task EnrichDomains_SetsRequiresAnswer_WhenRequiredQuestionMissing()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "years",
            NameAr = "سنوات",
            NameEn = "Years",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new TeacherDomainQuestionStatusService(
            questionRepo.Object,
            submissionRepo.Object,
            new TeacherDomainQuestionProvider(),
            Mock.Of<ISubjectService>());

        var domains = new List<EducationDomainDto>
        {
            new() { Id = DomainId, NameAr = "مدرسة", NameEn = "School", Code = "school", CreatedAt = DateTime.UtcNow }
        };

        var result = await service.EnrichDomainsForTeacherAsync(domains, TeacherId);

        Assert.True(result[0].RequiresAnswer);
        Assert.Single(result[0].Questions);
        Assert.False(result[0].Questions[0].IsSubmitted);
    }

    [Fact]
    public async Task ValidateSubjects_ReturnsError_WhenRequiredQuestionNotAnswered()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "years",
            NameAr = "سنوات",
            NameEn = "Years",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var subjectService = new Mock<ISubjectService>();
        subjectService
            .Setup(s => s.GetDomainsForSubjectIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([
                new SubjectDomainInfo
                {
                    SubjectId = 12,
                    DomainId = DomainId,
                    DomainCode = "school",
                    DomainNameEn = "School",
                    DomainNameAr = "مدرسة"
                }
            ]);

        var service = new TeacherDomainQuestionStatusService(
            questionRepo.Object,
            submissionRepo.Object,
            new TeacherDomainQuestionProvider(),
            subjectService.Object);

        var error = await service.ValidateSubjectsDomainQuestionsAsync(TeacherId, [12]);

        Assert.NotNull(error);
        Assert.Contains("school", error, StringComparison.OrdinalIgnoreCase);
    }
}
