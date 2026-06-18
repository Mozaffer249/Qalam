using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
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
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 },
            onStatusUpdate: status => updatedStatus = status);

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(TeacherStatus.Active, updatedStatus);
    }

    [Fact]
    public async Task Activate_Fails_WhenAlreadyActive()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.Active,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 });

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.False(success);
        Assert.Contains("already active", error, StringComparison.OrdinalIgnoreCase);
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

    private static TeacherRegistrationCompletionService BuildService(
        TeacherStatus teacherStatus,
        bool requirementsApproved,
        TeacherSubjectActivationSnapshot snapshot,
        Action<TeacherStatus>? onStatusUpdate = null)
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

        return new TeacherRegistrationCompletionService(
            requirementRepo.Object,
            submissionRepo.Object,
            documentRepo.Object,
            teacherRepo.Object,
            subjectRepo.Object,
            NullLogger<TeacherRegistrationCompletionService>.Instance);
    }
}
