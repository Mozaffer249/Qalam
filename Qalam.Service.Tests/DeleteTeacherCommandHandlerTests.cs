using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Core.Features.Admin.Commands.DeleteTeacher;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class DeleteTeacherCommandHandlerTests
{
    private const int TeacherId = 9;
    private const int AdminId = 1;

    [Fact]
    public async Task Handle_WhenServiceSucceeds_ReturnsSuccess()
    {
        var management = new Mock<ITeacherManagementService>();
        management
            .Setup(s => s.DeleteTeacherAccountAsync(TeacherId, AdminId, "cleanup", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Teacher account and related data deleted successfully"));

        var handler = CreateHandler(management.Object);
        var response = await handler.Handle(
            new DeleteTeacherCommand { TeacherId = TeacherId, UserId = AdminId, Reason = "cleanup" },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Contains("deleted", response.Data ?? response.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var management = new Mock<ITeacherManagementService>();
        management
            .Setup(s => s.DeleteTeacherAccountAsync(TeacherId, AdminId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Teacher not found"));

        var handler = CreateHandler(management.Object);
        var response = await handler.Handle(
            new DeleteTeacherCommand { TeacherId = TeacherId, UserId = AdminId },
            CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Equal(404, (int)response.StatusCode);
    }

    [Fact]
    public async Task Handle_WhenServiceSucceeds_AfterDualRoleWipe_ReturnsSuccess()
    {
        var management = new Mock<ITeacherManagementService>();
        management
            .Setup(s => s.DeleteTeacherAccountAsync(TeacherId, AdminId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Teacher account and related data deleted successfully"));

        var handler = CreateHandler(management.Object);
        var response = await handler.Handle(
            new DeleteTeacherCommand { TeacherId = TeacherId, UserId = AdminId },
            CancellationToken.None);

        Assert.True(response.Succeeded);
    }

    [Fact]
    public async Task ManagementService_DelegatesToAccountDeletionService()
    {
        var deletion = new Mock<ITeacherAccountDeletionService>();
        deletion
            .Setup(s => s.DeleteTeacherAccountAsync(TeacherId, AdminId, "r", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "ok"));

        var service = new TeacherManagementService(
            Mock.Of<ITeacherRepository>(),
            Mock.Of<ITeacherDocumentRepository>(),
            Mock.Of<ITeacherRegistrationCompletionService>(),
            Mock.Of<ITeacherLifecycleEmailService>(),
            deletion.Object,
            NullLogger<TeacherManagementService>.Instance);

        var (success, message) = await service.DeleteTeacherAccountAsync(TeacherId, AdminId, "r");

        Assert.True(success);
        Assert.Equal("ok", message);
        deletion.Verify(
            s => s.DeleteTeacherAccountAsync(TeacherId, AdminId, "r", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static DeleteTeacherCommandHandler CreateHandler(ITeacherManagementService management)
    {
        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        return new DeleteTeacherCommandHandler(
            management,
            NullLogger<DeleteTeacherCommandHandler>.Instance,
            localizer.Object);
    }
}
