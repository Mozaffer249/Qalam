using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.Features.Education.Commands.ToggleEducationDomainStatus;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;
using Xunit;

namespace Qalam.Service.Tests;

public class ToggleEducationDomainStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenDomainNotFound_ReturnsNotFound()
    {
        var domainService = new Mock<IEducationDomainService>();
        domainService.Setup(s => s.ToggleDomainStatusAsync(99)).ReturnsAsync(false);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var handler = new ToggleEducationDomainStatusCommandHandler(localizer.Object, domainService.Object);

        var response = await handler.Handle(
            new ToggleEducationDomainStatusCommand { Id = 99 },
            CancellationToken.None);

        Assert.False(response.Succeeded);
        domainService.Verify(s => s.ToggleDomainStatusAsync(99), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDomainExists_TogglesSuccessfully()
    {
        var domainService = new Mock<IEducationDomainService>();
        domainService.Setup(s => s.ToggleDomainStatusAsync(5)).ReturnsAsync(true);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var handler = new ToggleEducationDomainStatusCommandHandler(localizer.Object, domainService.Object);

        var response = await handler.Handle(
            new ToggleEducationDomainStatusCommand { Id = 5 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.True(response.Data);
    }
}
