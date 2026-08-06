using Qalam.Data.DTOs.Platform;

namespace Qalam.Service.Abstracts;

public interface IOsrNotificationSettingsProvider
{
    Task<OsrNotificationSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<OsrNotificationSettingsDto> SaveSettingsAsync(
        OsrNotificationSettingsDto settings,
        CancellationToken cancellationToken = default);
}
