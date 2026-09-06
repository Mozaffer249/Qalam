using Qalam.Data.DTOs.Account;

namespace Qalam.Service.Abstracts;

public interface IAccountDeactivationGuardService
{
    /// <summary>
    /// Returns machine-readable blocking reasons, or empty when deactivation is allowed.
    /// </summary>
    Task<IReadOnlyList<string>> GetBlockingReasonsAsync(int userId, CancellationToken cancellationToken = default);
}

public interface IAccountDeactivationService
{
    Task<(bool Success, string Message, IReadOnlyList<string>? BlockingReasons)> DeactivateAsync(
        int userId,
        string password,
        string? accessToken,
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}

public interface INotificationPreferenceService
{
    Task<NotificationPreferencesDto> GetAsync(int userId, CancellationToken cancellationToken = default);
    Task<NotificationPreferencesDto> UpdateAsync(int userId, NotificationPreferencesDto dto, CancellationToken cancellationToken = default);
    Task<bool> ShouldSendAsync(int userId, NotificationChannel channel, CancellationToken cancellationToken = default);
}

public enum NotificationChannel
{
    Push,
    EmailDigest,
    SmsAlerts
}
