using Moq;
using Qalam.Data.DTOs.Account;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class NotificationPreferenceServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsDefaultsFromRepository()
    {
        var repo = new Mock<IUserNotificationPreferencesRepository>();
        repo.Setup(r => r.GetOrCreateAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserNotificationPreferences
            {
                UserId = 7,
                PushEnabled = true,
                EmailDigestEnabled = true,
                SmsAlertsEnabled = false
            });

        var service = new NotificationPreferenceService(repo.Object);
        var dto = await service.GetAsync(7);

        Assert.True(dto.PushEnabled);
        Assert.True(dto.EmailDigestEnabled);
        Assert.False(dto.SmsAlertsEnabled);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAndReturnsValues()
    {
        var entity = new UserNotificationPreferences
        {
            UserId = 7,
            PushEnabled = true,
            EmailDigestEnabled = true,
            SmsAlertsEnabled = false
        };

        var repo = new Mock<IUserNotificationPreferencesRepository>();
        repo.Setup(r => r.GetOrCreateAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(r => r.UpdatePreferencesAsync(entity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new NotificationPreferenceService(repo.Object);
        var dto = await service.UpdateAsync(7, new NotificationPreferencesDto
        {
            PushEnabled = false,
            EmailDigestEnabled = false,
            SmsAlertsEnabled = true
        });

        Assert.False(dto.PushEnabled);
        Assert.False(dto.EmailDigestEnabled);
        Assert.True(dto.SmsAlertsEnabled);
        Assert.False(entity.PushEnabled);
        Assert.True(entity.SmsAlertsEnabled);
        repo.Verify(r => r.UpdatePreferencesAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(NotificationChannel.Push, true, true, false, true)]
    [InlineData(NotificationChannel.EmailDigest, true, false, false, false)]
    [InlineData(NotificationChannel.SmsAlerts, true, true, true, true)]
    public async Task ShouldSendAsync_RespectsChannelFlags(
        NotificationChannel channel,
        bool push,
        bool email,
        bool sms,
        bool expected)
    {
        var repo = new Mock<IUserNotificationPreferencesRepository>();
        repo.Setup(r => r.GetOrCreateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserNotificationPreferences
            {
                UserId = 1,
                PushEnabled = push,
                EmailDigestEnabled = email,
                SmsAlertsEnabled = sms
            });

        var service = new NotificationPreferenceService(repo.Object);
        Assert.Equal(expected, await service.ShouldSendAsync(1, channel));
    }
}
