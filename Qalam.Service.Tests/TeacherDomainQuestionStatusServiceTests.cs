using Moq;
using Qalam.Data.DTOs;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;
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
            Mock.Of<ISubjectService>(),
            Mock.Of<ITeacherSubjectRepository>(),
            Mock.Of<IEducationDomainService>(),
            Mock.Of<ITeacherDomainSubjectCascadeService>());

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
    public async Task EnrichDomains_SetsCanSelectForSubjects_FromCascadeService()
    {
        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var cascadeService = new Mock<ITeacherDomainSubjectCascadeService>();
        cascadeService
            .Setup(s => s.IsDomainFullyApprovedForTeacherAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        cascadeService
            .Setup(s => s.IsDomainFullyApprovedForTeacherAsync(TeacherId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new TeacherDomainQuestionStatusService(
            questionRepo.Object,
            submissionRepo.Object,
            new TeacherDomainQuestionProvider(),
            Mock.Of<ISubjectService>(),
            Mock.Of<ITeacherSubjectRepository>(),
            Mock.Of<IEducationDomainService>(),
            cascadeService.Object);

        var domains = new List<EducationDomainDto>
        {
            new() { Id = DomainId, NameAr = "مدرسة", NameEn = "School", Code = "school", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, NameAr = "قرآن", NameEn = "Quran", Code = "quran", CreatedAt = DateTime.UtcNow }
        };

        var result = await service.EnrichDomainsForTeacherAsync(domains, TeacherId);

        Assert.True(result[0].CanSelectForSubjects);
        Assert.False(result[1].CanSelectForSubjects);
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

        var cascadeService = new Mock<ITeacherDomainSubjectCascadeService>();
        cascadeService
            .Setup(s => s.GetSubjectSaveBlockReasonForDomainAsync(
                TeacherId, DomainId, "School", "school", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Complete domain questions for 'School' (school) before adding subjects.");

        var service = new TeacherDomainQuestionStatusService(
            questionRepo.Object,
            submissionRepo.Object,
            new TeacherDomainQuestionProvider(),
            subjectService.Object,
            Mock.Of<ITeacherSubjectRepository>(),
            Mock.Of<IEducationDomainService>(),
            cascadeService.Object);

        var error = await service.ValidateSubjectsDomainQuestionsAsync(TeacherId, [12]);

        Assert.NotNull(error);
        Assert.Contains("school", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnrichDomains_SetsRequiresAnswer_WhenRequiredQuestionRejected()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "license",
            NameAr = "رخصة",
            NameEn = "License",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    Id = 5,
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Rejected,
                    RejectionReason = "Expired"
                }
            ]);

        var service = new TeacherDomainQuestionStatusService(
            questionRepo.Object,
            submissionRepo.Object,
            new TeacherDomainQuestionProvider(),
            Mock.Of<ISubjectService>(),
            Mock.Of<ITeacherSubjectRepository>(),
            Mock.Of<IEducationDomainService>(),
            Mock.Of<ITeacherDomainSubjectCascadeService>());

        var domains = new List<EducationDomainDto>
        {
            new() { Id = DomainId, NameAr = "مدرسة", NameEn = "School", Code = "school", CreatedAt = DateTime.UtcNow }
        };

        var result = await service.EnrichDomainsForTeacherAsync(domains, TeacherId);

        Assert.True(result[0].RequiresAnswer);
        Assert.Equal("Expired", result[0].Questions[0].RejectionReason);
    }

    [Fact]
    public async Task HasIncompleteCatalogDomainAnswers_ReturnsTrue_WhenRequiredQuestionMissing()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "years",
            IsActive = true,
            IsRequired = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DomainId]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = BuildStatusService(questionRepo.Object, submissionRepo.Object, Mock.Of<ITeacherDomainSubjectCascadeService>());

        Assert.True(await service.HasIncompleteCatalogDomainAnswersAsync(TeacherId));
    }

    [Fact]
    public async Task HasAnyAnswersPendingAdminReview_ReturnsTrue_WhenRequiredSubmissionPending()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "license",
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DomainId]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Pending
                }
            ]);

        var service = BuildStatusService(questionRepo.Object, submissionRepo.Object, Mock.Of<ITeacherDomainSubjectCascadeService>());

        Assert.True(await service.HasAnyAnswersPendingAdminReviewAsync(TeacherId));
    }

    [Fact]
    public async Task HasAnyAnswersPendingAdminReview_ReturnsFalse_WhenAllApproved()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "years",
            IsActive = true,
            IsRequired = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DomainId]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Approved
                }
            ]);

        var service = BuildStatusService(questionRepo.Object, submissionRepo.Object, Mock.Of<ITeacherDomainSubjectCascadeService>());

        Assert.False(await service.HasAnyAnswersPendingAdminReviewAsync(TeacherId));
    }

    [Fact]
    public async Task HasIncompleteCatalogDomainAnswers_ReturnsTrue_WhenOnlyRejectedExists()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "license",
            IsActive = true,
            IsRequired = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DomainId]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Rejected
                }
            ]);

        var service = BuildStatusService(questionRepo.Object, submissionRepo.Object, Mock.Of<ITeacherDomainSubjectCascadeService>());

        Assert.True(await service.HasIncompleteCatalogDomainAnswersAsync(TeacherId));
    }

    [Fact]
    public async Task HasCatalogDomainsPendingAdminReview_ReturnsTrue_WhenSubmittedButNotApproved()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "license",
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DomainId]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Pending
                }
            ]);

        var cascade = new Mock<ITeacherDomainSubjectCascadeService>();
        cascade
            .Setup(s => s.IsDomainFullyApprovedForTeacherAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = BuildStatusService(questionRepo.Object, submissionRepo.Object, cascade.Object);

        Assert.True(await service.HasCatalogDomainsPendingAdminReviewAsync(TeacherId));
    }

    [Fact]
    public async Task HasIncompleteCatalogDomainAnswers_ReturnsTrue_WhenOneOfTwoDomainsFullySubmitted()
    {
        const int domain2 = 2;
        var schoolQuestion = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "years",
            IsActive = true,
            IsRequired = true
        };
        var quranQuestion = new TeacherDomainQuestion
        {
            Id = 2,
            DomainId = domain2,
            Code = "ijaza",
            IsActive = true,
            IsRequired = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DomainId, domain2]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([schoolQuestion]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(domain2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([quranQuestion]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Pending
                }
            ]);

        var service = BuildStatusService(questionRepo.Object, submissionRepo.Object, Mock.Of<ITeacherDomainSubjectCascadeService>());

        Assert.True(await service.HasIncompleteCatalogDomainAnswersAsync(TeacherId));
    }

    [Fact]
    public async Task HasIncompleteCatalogDomainAnswers_ReturnsTrue_WhenNeitherDomainFullySubmitted()
    {
        const int domain2 = 2;
        var schoolQuestion = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "years",
            IsActive = true,
            IsRequired = true
        };
        var quranQuestion = new TeacherDomainQuestion
        {
            Id = 2,
            DomainId = domain2,
            Code = "ijaza",
            IsActive = true,
            IsRequired = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DomainId, domain2]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([schoolQuestion]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(domain2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([quranQuestion]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = BuildStatusService(questionRepo.Object, submissionRepo.Object, Mock.Of<ITeacherDomainSubjectCascadeService>());

        Assert.True(await service.HasIncompleteCatalogDomainAnswersAsync(TeacherId));
    }

    [Fact]
    public async Task HasAnyAnswersPendingAdminReview_ReturnsTrue_WhenOptionalAdminReviewPending()
    {
        var required = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "years",
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = false
        };
        var optional = new TeacherDomainQuestion
        {
            Id = 2,
            DomainId = DomainId,
            Code = "cert",
            IsActive = true,
            IsRequired = false,
            RequiresAdminReview = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DomainId]);
        questionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([required, optional]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Approved
                },
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 2,
                    VerificationStatus = DocumentVerificationStatus.Pending
                }
            ]);

        var service = BuildStatusService(questionRepo.Object, submissionRepo.Object, Mock.Of<ITeacherDomainSubjectCascadeService>());

        Assert.True(await service.HasAnyAnswersPendingAdminReviewAsync(TeacherId));
    }

    [Fact]
    public async Task GetRejectedDomainCorrections_ReturnsRejectedSubmissions()
    {
        const int submissionId = 15;
        var question = new TeacherDomainQuestion
        {
            Id = 20,
            DomainId = 4,
            Code = "skills_certification",
            NameEn = "Professional or training certification",
            Domain = new EducationDomain { Id = 4, Code = "skills", NameEn = "General Skills" }
        };

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdWithQuestionsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    Id = submissionId,
                    TeacherId = TeacherId,
                    QuestionId = question.Id,
                    Question = question,
                    VerificationStatus = DocumentVerificationStatus.Rejected,
                    RejectionReason = "Invalid certificate"
                }
            ]);

        var service = BuildStatusService(
            Mock.Of<ITeacherDomainQuestionRepository>(),
            submissionRepo.Object,
            Mock.Of<ITeacherDomainSubjectCascadeService>());

        var corrections = await service.GetRejectedDomainCorrectionsAsync(TeacherId);

        Assert.Single(corrections);
        Assert.Equal(TeacherReviewCorrectionType.DomainQuestion, corrections[0].Type);
        Assert.Equal(submissionId, corrections[0].SubmissionId);
        Assert.Equal(4, corrections[0].DomainId);
        Assert.Equal("skills", corrections[0].DomainCode);
        Assert.Equal("Invalid certificate", corrections[0].RejectionReason);
    }

    private static TeacherDomainQuestionStatusService BuildStatusService(
        ITeacherDomainQuestionRepository questionRepo,
        ITeacherDomainQuestionSubmissionRepository submissionRepo,
        ITeacherDomainSubjectCascadeService cascadeService) =>
        new(
            questionRepo,
            submissionRepo,
            new TeacherDomainQuestionProvider(),
            Mock.Of<ISubjectService>(),
            Mock.Of<ITeacherSubjectRepository>(),
            Mock.Of<IEducationDomainService>(),
            cascadeService);
}
