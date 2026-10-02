using Qalam.Data.DTOs.Notifications;

namespace Qalam.Service.Abstracts;

public interface IUserNotificationService
{
    Task<UserNotificationsPageDto> ListAsync(int userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> UnreadCountAsync(int userId, CancellationToken cancellationToken = default);
}
