using Moq;
using Qalam.Infrastructure.Abstracts;
using Xunit;

namespace Qalam.Service.Tests;

public class UserDeviceTokenRepositoryContractTests
{
    [Fact]
    public async Task Upsert_ThenUnregister_UsesRepositoryMethods()
    {
        var repo = new Mock<IUserDeviceTokenRepository>();
        repo.Setup(r => r.UpsertAsync(3, "tok-a", "android", "1.0", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.DeactivateAsync(3, "tok-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.DeactivateAllForUserAsync(3, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.GetActiveTokensForUserAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "tok-a" });

        await repo.Object.UpsertAsync(3, "tok-a", "android", "1.0");
        await repo.Object.UpsertAsync(3, "tok-a", "android", "1.0"); // re-register same token
        var active = await repo.Object.GetActiveTokensForUserAsync(3);
        Assert.Single(active);

        Assert.True(await repo.Object.DeactivateAsync(3, "tok-a"));
        await repo.Object.DeactivateAllForUserAsync(3);

        repo.Verify(r => r.UpsertAsync(3, "tok-a", "android", "1.0", It.IsAny<CancellationToken>()), Times.Exactly(2));
        repo.Verify(r => r.DeactivateAsync(3, "tok-a", It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.DeactivateAllForUserAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }
}
