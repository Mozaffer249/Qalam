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
    public async Task CanActivate_ReturnsFalse_WhenNoSubjects()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 });

        Assert.False(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsFalse_WhenSubjectPending()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Pending = 1 });

        Assert.False(await service.CanActivateTeacherAccountAsync(TeacherId));
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

    private static TeacherRegistrationCompletionService BuildService(
        TeacherStatus teacherStatus,
        bool requirementsApproved,
        TeacherSubjectActivationSnapshot snapshot,
        Action<TeacherStatus>? onStatusUpdate = null,
        ITeacherLifecycleEmailService? lifecycleEmail = null,
        List<int>? domainIds = null,
        List<TeacherDomainQuestion>? domainQuestions = null,
        List<TeacherDomainQuestionSubmission>? domainSubmissions = null)
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

        var requirementRepo = new Mock<ITeacherRegistrationRequirementRepository>();
        requirementRepo
            .Setup(r => r.GetActiveOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([requirement]);

        var submissionRepo = new Mock<ITeacherRegistrationSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdWithRequirementsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([submission]);

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

        var subjectRepo = new Mock<ITeacherSubjectRepository>();
        subjectRepo
            .Setup(r => r.GetSubjectActivationSnapshotAsync(TeacherId))
            .ReturnsAsync(snapshot);
        subjectRepo
            .Setup(r => r.GetDistinctDomainIdsForTeacherAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainIds ?? []);

        var domainQuestionRepo = new Mock<ITeacherDomainQuestionRepository>();
        domainQuestionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainQuestions ?? []);

        var domainSubmissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        domainSubmissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainSubmissions ?? []);

        return new TeacherRegistrationCompletionService(
            requirementRepo.Object,
            submissionRepo.Object,
            documentRepo.Object,
            teacherRepo.Object,
            subjectRepo.Object,
            domainQuestionRepo.Object,
            domainSubmissionRepo.Object,
            lifecycleEmail ?? Mock.Of<ITeacherLifecycleEmailService>(),
            NullLogger<TeacherRegistrationCompletionService>.Instance);
    }
}
