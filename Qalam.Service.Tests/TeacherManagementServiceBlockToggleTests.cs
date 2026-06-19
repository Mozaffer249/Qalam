using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class TeacherManagementServiceBlockToggleTests
{
    private const int TeacherId = 99;
    private const int AdminId = 1;

    [Fact]
    public async Task ToggleBlock_FromActive_BlocksAndStoresPreviousStatus()
    {
        var teacher = new Teacher
        {
            Id = TeacherId,
            Status = TeacherStatus.Active,
            IsActive = true
        };

        var (service, lifecycleEmail, teacherRepo) = BuildService(teacher);

        var (success, isBlocked, message) = await service.ToggleBlockTeacherAsync(TeacherId, AdminId, "test");

        Assert.True(success);
        Assert.True(isBlocked);
        Assert.Contains("blocked", message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TeacherStatus.Blocked, teacher.Status);
        Assert.False(teacher.IsActive);
        Assert.Equal(TeacherStatus.Active, teacher.StatusBeforeBlock);

        lifecycleEmail.Verify(
            e => e.SendAccountBlockedAsync(TeacherId, "test", It.IsAny<CancellationToken>()),
            Times.Once);
        teacherRepo.Verify(r => r.UpdateAsync(teacher), Times.Once);
    }

    [Fact]
    public async Task ToggleBlock_FromBlocked_RestoresActiveAndUnblocks()
    {
        var teacher = new Teacher
        {
            Id = TeacherId,
            Status = TeacherStatus.Blocked,
            IsActive = false,
            StatusBeforeBlock = TeacherStatus.Active
        };

        var (service, lifecycleEmail, _) = BuildService(teacher);

        var (success, isBlocked, message) = await service.ToggleBlockTeacherAsync(TeacherId, AdminId, null);

        Assert.True(success);
        Assert.False(isBlocked);
        Assert.Contains("unblocked", message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TeacherStatus.Active, teacher.Status);
        Assert.True(teacher.IsActive);
        Assert.Null(teacher.StatusBeforeBlock);

        lifecycleEmail.Verify(
            e => e.SendAccountUnblockedAsync(TeacherId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ToggleBlock_FromBlocked_RestoresPendingVerificationWhenNoSnapshot()
    {
        var teacher = new Teacher
        {
            Id = TeacherId,
            Status = TeacherStatus.Blocked,
            IsActive = false,
            StatusBeforeBlock = null
        };

        var (service, _, _) = BuildService(teacher);

        var (_, isBlocked, _) = await service.ToggleBlockTeacherAsync(TeacherId, AdminId, null);

        Assert.False(isBlocked);
        Assert.Equal(TeacherStatus.PendingVerification, teacher.Status);
        Assert.False(teacher.IsActive);
    }

    private static (
        TeacherManagementService Service,
        Mock<ITeacherLifecycleEmailService> LifecycleEmail,
        Mock<ITeacherRepository> TeacherRepo) BuildService(Teacher teacher)
    {
        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(TeacherId)).ReturnsAsync(teacher);
        teacherRepo.Setup(r => r.UpdateAsync(It.IsAny<Teacher>())).Returns(Task.CompletedTask);
        teacherRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var documentRepo = new Mock<ITeacherDocumentRepository>();
        var completionService = new Mock<ITeacherRegistrationCompletionService>();
        var lifecycleEmail = new Mock<ITeacherLifecycleEmailService>();

        var service = new TeacherManagementService(
            teacherRepo.Object,
            documentRepo.Object,
            completionService.Object,
            lifecycleEmail.Object,
            NullLogger<TeacherManagementService>.Instance);

        return (service, lifecycleEmail, teacherRepo);
    }
}
