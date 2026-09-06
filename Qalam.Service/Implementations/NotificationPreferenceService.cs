using Qalam.Data.DTOs.Account;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly IUserNotificationPreferencesRepository _repository;

    public NotificationPreferenceService(IUserNotificationPreferencesRepository repository)
    {
        _repository = repository;
    }

    public async Task<NotificationPreferencesDto> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetOrCreateAsync(userId, cancellationToken);
        return Map(entity);
    }

    public async Task<NotificationPreferencesDto> UpdateAsync(
        int userId,
        NotificationPreferencesDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetOrCreateAsync(userId, cancellationToken);
        entity.PushEnabled = dto.PushEnabled;
        entity.EmailDigestEnabled = dto.EmailDigestEnabled;
        entity.SmsAlertsEnabled = dto.SmsAlertsEnabled;
        await _repository.UpdatePreferencesAsync(entity, cancellationToken);
        return Map(entity);
    }

    public async Task<bool> ShouldSendAsync(
        int userId,
        NotificationChannel channel,
        CancellationToken cancellationToken = default)
    {
        var prefs = await GetAsync(userId, cancellationToken);
        return channel switch
        {
            NotificationChannel.Push => prefs.PushEnabled,
            NotificationChannel.EmailDigest => prefs.EmailDigestEnabled,
            NotificationChannel.SmsAlerts => prefs.SmsAlertsEnabled,
            _ => true
        };
    }

    private static NotificationPreferencesDto Map(Data.Entity.Identity.UserNotificationPreferences entity) => new()
    {
        PushEnabled = entity.PushEnabled,
        EmailDigestEnabled = entity.EmailDigestEnabled,
        SmsAlertsEnabled = entity.SmsAlertsEnabled
    };
}
