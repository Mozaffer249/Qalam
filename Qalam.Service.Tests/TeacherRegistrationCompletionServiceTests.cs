using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class TeacherRegistrationCompletionServiceTests
{
    private const int TeacherId = 42;
    private const int AdminId = 1;

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenNoSubjects()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 });

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenSubjectPendingButDomainApproved()
    {
        var domainQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            IsRequired = true,
            RequiresAdminReview = true
        };

        var domainSubmission = new TeacherDomainQuestionSubmission
        {
            Id = 100,
            TeacherId = TeacherId,
            QuestionId = domainQuestion.Id,
            Question = domainQuestion,
            VerificationStatus = DocumentVerificationStatus.Approved
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Pending = 1 },
            domainIds: [1],
            domainQuestions: [domainQuestion],
            domainSubmissions: [domainSubmission]);

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenDocsAndSubjectsApproved()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 2, Approved = 2 });

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task Activate_Succeeds_WhenReady()
    {
        TeacherStatus? updatedStatus = null;
        var lifecycleEmail = new Mock<ITeacherLifecycleEmailService>();
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 },
            onStatusUpdate: status => updatedStatus = status,
            lifecycleEmail: lifecycleEmail.Object);

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(TeacherStatus.Active, updatedStatus);
        lifecycleEmail.Verify(
            e => e.SendAccountActivatedAsync(TeacherId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Activate_Fails_WhenAlreadyActive()
    {
        var lifecycleEmail = new Mock<ITeacherLifecycleEmailService>();
        var service = BuildService(
            teacherStatus: TeacherStatus.Active,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 },
            lifecycleEmail: lifecycleEmail.Object);

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.False(success);
        Assert.Contains("already active", error, StringComparison.OrdinalIgnoreCase);
        lifecycleEmail.Verify(
            e => e.SendAccountActivatedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Activate_Fails_WhenBlocked()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.Blocked,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 });

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.False(success);
        Assert.Contains("blocked", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Activate_Fails_WhenDocumentStillPending()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: false,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 });

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.False(success);
        Assert.Contains("pending", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshTeacherStatus_DoesNotAutoActivate_WhenAllApproved()
    {
        TeacherStatus? updatedStatus = null;
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 },
            onStatusUpdate: status => updatedStatus = status);

        await service.RefreshTeacherStatusAfterReviewAsync(TeacherId);

        Assert.Equal(TeacherStatus.PendingVerification, updatedStatus);
    }

    [Fact]
    public async Task CanActivate_ReturnsFalse_WhenDomainQuestionRequiresReviewAndPending()
    {
        var domainQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            NameAr = "خبرة",
            NameEn = "Experience",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = true
        };

        var domainSubmission = new TeacherDomainQuestionSubmission
        {
            Id = 100,
            TeacherId = TeacherId,
            QuestionId = domainQuestion.Id,
            Question = domainQuestion,
            VerificationStatus = DocumentVerificationStatus.Pending,
            TextValue = "5 years"
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 },
            domainIds: [1],
            domainQuestions: [domainQuestion],
            domainSubmissions: [domainSubmission]);

        Assert.False(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenDomainQuestionAutoApproved()
    {
        var domainQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            NameAr = "خبرة",
            NameEn = "Experience",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = false
        };

        var domainSubmission = new TeacherDomainQuestionSubmission
        {
            Id = 100,
            TeacherId = TeacherId,
            QuestionId = domainQuestion.Id,
            Question = domainQuestion,
            VerificationStatus = DocumentVerificationStatus.Approved,
            TextValue = "5 years"
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 },
            domainIds: [1],
            domainQuestions: [domainQuestion],
            domainSubmissions: [domainSubmission]);

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenOneOfTwoDomainsFullyApproved()
    {
        const int domain2 = 2;
        var schoolQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            IsRequired = true,
            RequiresAdminReview = true
        };
        var quranQuestion = new TeacherDomainQuestion
        {
            Id = 11,
            DomainId = domain2,
            Code = "quran_ijaza",
            IsRequired = true,
            RequiresAdminReview = true
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, domain2],
            domainQuestions: [schoolQuestion, quranQuestion],
            domainSubmissions:
            [
                new TeacherDomainQuestionSubmission
                {
                    Id = 100,
                    TeacherId = TeacherId,
                    QuestionId = schoolQuestion.Id,
                    Question = schoolQuestion,
                    VerificationStatus = DocumentVerificationStatus.Approved
                }
            ]);

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsFalse_WhenAnyDomainSubmissionRejected()
    {
        const int domain2 = 2;
        var schoolQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            IsRequired = true,
            RequiresAdminReview = true
        };
        var licenseQuestion = new TeacherDomainQuestion
        {
            Id = 11,
            DomainId = 1,
            Code = "school_teaching_license",
            IsRequired = false,
            RequiresAdminReview = true
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, domain2],
            domainQuestions: [schoolQuestion, licenseQuestion],
            domainSubmissions:
            [
                new TeacherDomainQuestionSubmission
                {
                    Id = 100,
                    TeacherId = TeacherId,
                    QuestionId = schoolQuestion.Id,
                    Question = schoolQuestion,
                    VerificationStatus = DocumentVerificationStatus.Approved
                },
                new TeacherDomainQuestionSubmission
                {
                    Id = 101,
                    TeacherId = TeacherId,
                    QuestionId = licenseQuestion.Id,
                    Question = licenseQuestion,
                    VerificationStatus = DocumentVerificationStatus.Rejected,
                    RejectionReason = "Expired"
                }
            ]);

        Assert.False(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task Activate_Fails_WhenDocumentsRejectedStatus()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.DocumentsRejected,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 });

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.False(success);
        Assert.Contains("corrected", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CanActivate_DoesNotThrow_WithMultipleCertificateSubmissions()
    {
        var identityRequirement = new TeacherRegistrationRequirement
        {
            Id = 1,
            Code = "identity",
            NameAr = "identity",
            NameEn = "identity",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 1
        };

        var certificateRequirement = new TeacherRegistrationRequirement
        {
            Id = 2,
            Code = "certificate",
            NameAr = "certificate",
            NameEn = "certificate",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 5
        };

        var submissions = new List<TeacherRegistrationSubmission>
        {
            new()
            {
                Id = 1,
                TeacherId = TeacherId,
                RequirementId = identityRequirement.Id,
                Requirement = identityRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            },
            new()
            {
                Id = 2,
                TeacherId = TeacherId,
                RequirementId = certificateRequirement.Id,
                Requirement = certificateRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            },
            new()
            {
                Id = 3,
                TeacherId = TeacherId,
                RequirementId = certificateRequirement.Id,
                Requirement = certificateRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            requirements: [identityRequirement, certificateRequirement],
            registrationSubmissions: submissions);

        var canActivate = await service.CanActivateTeacherAccountAsync(TeacherId);

        Assert.True(canActivate);
    }

    [Fact]
    public async Task RefreshTeacherStatusAfterReview_SetsDocumentsRejected_WhenDomainRejectedAndMultiFileCerts()
    {
        TeacherStatus? updatedStatus = null;

        var identityRequirement = new TeacherRegistrationRequirement
        {
            Id = 1,
            Code = "identity",
            NameAr = "identity",
            NameEn = "identity",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 1
        };

        var certificateRequirement = new TeacherRegistrationRequirement
        {
            Id = 2,
            Code = "certificate",
            NameAr = "certificate",
            NameEn = "certificate",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 5
        };

        var domainQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "skills_certification",
            IsRequired = false,
            RequiresAdminReview = true
        };

        var submissions = new List<TeacherRegistrationSubmission>
        {
            new()
            {
                Id = 1,
                TeacherId = TeacherId,
                RequirementId = identityRequirement.Id,
                Requirement = identityRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            },
            new()
            {
                Id = 2,
                TeacherId = TeacherId,
                RequirementId = certificateRequirement.Id,
                Requirement = certificateRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            },
            new()
            {
                Id = 3,
                TeacherId = TeacherId,
                RequirementId = certificateRequirement.Id,
                Requirement = certificateRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            requirements: [identityRequirement, certificateRequirement],
            registrationSubmissions: submissions,
            domainIds: [1],
            domainQuestions: [domainQuestion],
            domainSubmissions:
            [
                new TeacherDomainQuestionSubmission
                {
                    Id = 100,
                    TeacherId = TeacherId,
                    QuestionId = domainQuestion.Id,
                    Question = domainQuestion,
                    VerificationStatus = DocumentVerificationStatus.Rejected,
                    RejectionReason = "Invalid certificate"
                }
            ],
            onStatusUpdate: status => updatedStatus = status);

        await service.RefreshTeacherStatusAfterReviewAsync(TeacherId);

        Assert.Equal(TeacherStatus.DocumentsRejected, updatedStatus);
    }

    private static TeacherRegistrationCompletionService BuildService(
        TeacherStatus teacherStatus,
        bool requirementsApproved,
        TeacherSubjectActivationSnapshot snapshot,
        Action<TeacherStatus>? onStatusUpdate = null,
        ITeacherLifecycleEmailService? lifecycleEmail = null,
        List<int>? domainIds = null,
        List<TeacherDomainQuestion>? domainQuestions = null,
        List<TeacherDomainQuestionSubmission>? domainSubmissions = null,
        List<TeacherRegistrationRequirement>? requirements = null,
        List<TeacherRegistrationSubmission>? registrationSubmissions = null)
    {
        var teacher = new Teacher { Id = TeacherId, Status = teacherStatus };

        var requirement = new TeacherRegistrationRequirement
        {
            Id = 1,
            Code = "identity",
            NameAr = "identity",
            NameEn = "identity",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 1
        };

        var submission = new TeacherRegistrationSubmission
        {
            Id = 1,
            TeacherId = TeacherId,
            RequirementId = requirement.Id,
            Requirement = requirement,
            VerificationStatus = requirementsApproved
                ? DocumentVerificationStatus.Approved
                : DocumentVerificationStatus.Pending
        };

        var activeRequirements = requirements ?? [requirement];
        var activeSubmissions = registrationSubmissions ?? [submission];

        var requirementRepo = new Mock<ITeacherRegistrationRequirementRepository>();
        requirementRepo
            .Setup(r => r.GetActiveOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeRequirements);

        var submissionRepo = new Mock<ITeacherRegistrationSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdWithRequirementsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeSubmissions);

        var documentRepo = new Mock<ITeacherDocumentRepository>();
        documentRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId))
            .ReturnsAsync([]);

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(TeacherId)).ReturnsAsync(teacher);
        teacherRepo
            .Setup(r => r.UpdateStatusAsync(TeacherId, It.IsAny<TeacherStatus>()))
            .Callback<int, TeacherStatus>((_, status) =>
            {
                teacher.Status = status;
                onStatusUpdate?.Invoke(status);
            })
            .Returns(Task.CompletedTask);
        teacherRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var domainQuestionRepo = new Mock<ITeacherDomainQuestionRepository>();
        domainQuestionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainIds ?? []);
        domainQuestionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainQuestions ?? []);

        var domainSubmissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        domainSubmissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainSubmissions ?? []);
        domainSubmissionRepo
            .Setup(r => r.GetByTeacherIdWithQuestionsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainSubmissions ?? []);

        return new TeacherRegistrationCompletionService(
            requirementRepo.Object,
            submissionRepo.Object,
            documentRepo.Object,
            teacherRepo.Object,
            domainQuestionRepo.Object,
            domainSubmissionRepo.Object,
            lifecycleEmail ?? Mock.Of<ITeacherLifecycleEmailService>(),
            NullLogger<TeacherRegistrationCompletionService>.Instance);
    }
}
