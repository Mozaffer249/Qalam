using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Notifications;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class UserNotificationService : IUserNotificationService
{
    private readonly ApplicationDBContext _db;

    public UserNotificationService(ApplicationDBContext db)
    {
        _db = db;
    }

    public async Task<UserNotificationsPageDto> ListAsync(
        int userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.UserNotifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new UserNotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                TitleAr = n.TitleAr,
                TitleEn = n.TitleEn,
                BodyAr = n.BodyAr,
                BodyEn = n.BodyEn,
                DataJson = n.DataJson,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new UserNotificationsPageDto
        {
            Items = items,
            TotalCount = total,
            UnreadCount = await UnreadCountAsync(userId, cancellationToken),
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> MarkReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (row == null)
            return false;
        if (!row.IsRead)
        {
            row.IsRead = true;
            row.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    public async Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.IsRead = true;
            row.ReadAt = now;
        }
        if (rows.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    public Task<int> UnreadCountAsync(int userId, CancellationToken cancellationToken = default)
        => _db.UserNotifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
}
