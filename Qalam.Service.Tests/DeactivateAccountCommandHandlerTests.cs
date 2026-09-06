using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.Features.Authentication.Commands.DeactivateAccount;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.DTOs.Account;
using Qalam.Service.Abstracts;
using Xunit;

namespace Qalam.Service.Tests;

public class DeactivateAccountCommandHandlerTests
{
    private static DeactivateAccountCommandHandler Create(Mock<IAccountDeactivationService> service)
    {
        var localizer = new Mock<IStringLocalizer<AuthenticationResources>>();
        localizer.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        return new DeactivateAccountCommandHandler(service.Object, localizer.Object);
    }

    [Fact]
    public async Task Handle_WhenSuccess_ReturnsSucceeded()
    {
        var service = new Mock<IAccountDeactivationService>();
        service.Setup(s => s.DeactivateAsync(9, "pass", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Account deactivated successfully", (IReadOnlyList<string>?)null));

        var handler = Create(service);
        var response = await handler.Handle(new DeactivateAccountCommand
        {
            UserId = 9,
            Password = "pass"
        }, CancellationToken.None);

        Assert.True(response.Succeeded);
    }

    [Fact]
    public async Task Handle_WhenWrongPassword_ReturnsBadRequest()
    {
        var service = new Mock<IAccountDeactivationService>();
        service.Setup(s => s.DeactivateAsync(9, "bad", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Password is incorrect", (IReadOnlyList<string>?)null));

        var handler = Create(service);
        var response = await handler.Handle(new DeactivateAccountCommand
        {
            UserId = 9,
            Password = "bad"
        }, CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Equal(400, (int)response.StatusCode);
    }

    [Fact]
    public async Task Handle_WhenBlocked_ReturnsMetaWithReasons()
    {
        var service = new Mock<IAccountDeactivationService>();
        service.Setup(s => s.DeactivateAsync(9, "pass", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Cannot deactivate", (IReadOnlyList<string>)new[]
            {
                AccountDeactivationBlockingReasons.ActiveEnrollment
            }));

        var handler = Create(service);
        var response = await handler.Handle(new DeactivateAccountCommand
        {
            UserId = 9,
            Password = "pass"
        }, CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Equal(400, (int)response.StatusCode);
        var meta = Assert.IsType<AccountDeactivationBlockedDto>(response.Meta);
        Assert.Contains(AccountDeactivationBlockingReasons.ActiveEnrollment, meta.BlockingReasons);
    }
}
