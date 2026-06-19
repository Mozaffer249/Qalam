using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Email;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class TeacherLifecycleEmailServiceTests
{
    private const int TeacherId = 7;
    private const string TeacherEmail = "teacher@example.com";

    [Fact]
    public async Task SendRegistrationReceived_UsesConfiguredLoginUrl()
    {
        var emailService = new Mock<IEmailService>();
        string? capturedBody = null;

        emailService
            .Setup(e => e.SendEmailAsync(
                TeacherEmail,
                It.IsAny<string>(),
                It.IsAny<string>(),
                SendingStrategy.Queued))
            .Callback<string, string, string, SendingStrategy>((_, _, body, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        var service = BuildService(emailService.Object, "http://localhost:3000/");

        await service.SendRegistrationReceivedAsync(TeacherId);

        emailService.Verify(
            e => e.SendEmailAsync(
                TeacherEmail,
                TeacherLifecycleEmailTemplates.BuildRegistrationReceivedSubject(),
                It.IsAny<string>(),
                SendingStrategy.Queued),
            Times.Once);

        Assert.NotNull(capturedBody);
        Assert.Contains("http://localhost:3000/", capturedBody);
    }

    [Fact]
    public async Task SendAccountBlocked_DoesNotIncludeLoginButton()
    {
        var emailService = new Mock<IEmailService>();
        string? capturedBody = null;

        emailService
            .Setup(e => e.SendEmailAsync(
                TeacherEmail,
                It.IsAny<string>(),
                It.IsAny<string>(),
                SendingStrategy.Queued))
            .Callback<string, string, string, SendingStrategy>((_, _, body, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        var service = BuildService(emailService.Object);

        await service.SendAccountBlockedAsync(TeacherId, "Policy violation");

        Assert.NotNull(capturedBody);
        Assert.DoesNotContain("Log in to Qalam", capturedBody);
        Assert.Contains("Policy violation", capturedBody);
    }

    [Fact]
    public async Task SendRegistrationReceived_SkipsWhenNoEmail()
    {
        var emailService = new Mock<IEmailService>();
        var service = BuildService(emailService.Object, hasEmail: false);

        await service.SendRegistrationReceivedAsync(TeacherId);

        emailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SendingStrategy>()),
            Times.Never);
    }

    [Fact]
    public async Task SendRegistrationReceived_DoesNotThrowWhenEmailFails()
    {
        var emailService = new Mock<IEmailService>();
        emailService
            .Setup(e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                SendingStrategy.Queued))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var service = BuildService(emailService.Object);

        await service.SendRegistrationReceivedAsync(TeacherId);
    }

    private static TeacherLifecycleEmailService BuildService(
        IEmailService emailService,
        string webAppBaseUrl = "https://qalam.net.sa/",
        bool hasEmail = true)
    {
        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo
            .Setup(r => r.GetEmailsByTeacherIdsAsync(
                It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(TeacherId)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasEmail
                ? new List<(int TeacherId, string Email)> { (TeacherId, TeacherEmail) }
                : []);

        return new TeacherLifecycleEmailService(
            teacherRepo.Object,
            emailService,
            Options.Create(new PlatformSettings { WebAppBaseUrl = webAppBaseUrl }),
            NullLogger<TeacherLifecycleEmailService>.Instance);
    }
}
