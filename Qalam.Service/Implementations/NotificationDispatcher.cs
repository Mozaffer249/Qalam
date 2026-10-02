using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly ApplicationDBContext _db;
    private readonly IRabbitMQService _rabbitMq;
    private readonly IPushNotificationService _push;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        ApplicationDBContext db,
        IRabbitMQService rabbitMq,
        IPushNotificationService push,
        ILogger<NotificationDispatcher> logger)
    {
        _db = db;
        _rabbitMq = rabbitMq;
        _push = push;
        _logger = logger;
    }

    public async Task NotifyAsync(IEnumerable<int> userIds, NotificationContent content, CancellationToken cancellationToken = default)
    {
        var recipients = userIds.Where(id => id > 0).Distinct().ToList();
        if (recipients.Count == 0)
            return;

        var dataJson = content.Data == null ? null : JsonSerializer.Serialize(content.Data);
        var rows = recipients.Select(userId => new UserNotification
        {
            UserId = userId,
            Type = Truncate(content.Type, 60),
            TitleAr = Truncate(content.TitleAr, 200),
            TitleEn = Truncate(content.TitleEn, 200),
            BodyAr = Truncate(content.BodyAr, 1000),
            BodyEn = Truncate(content.BodyEn, 1000),
            DataJson = dataJson,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        try
        {
            _db.UserNotifications.AddRange(rows);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            foreach (var row in rows)
                _db.Entry(row).State = EntityState.Detached;
            _logger.LogError(ex, "Failed to store {Type} notifications for users {UserIds}.", content.Type, recipients);
            return;
        }

        await SendEmailsAsync(recipients, content, cancellationToken);
        await SendPushAsync(recipients, content, cancellationToken);
    }

    private async Task SendEmailsAsync(List<int> recipients, NotificationContent content, CancellationToken cancellationToken)
    {
        try
        {
            var emails = await _db.Users.AsNoTracking()
                .Where(u => recipients.Contains(u.Id) && u.Email != null)
                .Select(u => u.Email!)
                .ToListAsync(cancellationToken);

            var body =
                $"<div dir=\"rtl\"><h3>{WebUtility.HtmlEncode(content.TitleAr)}</h3><p>{WebUtility.HtmlEncode(content.BodyAr)}</p></div>" +
                $"<hr/><div dir=\"ltr\"><h3>{WebUtility.HtmlEncode(content.TitleEn)}</h3><p>{WebUtility.HtmlEncode(content.BodyEn)}</p></div>";

            foreach (var email in emails)
            {
                await _rabbitMq.QueueEmailAsync(new EmailMessage
                {
                    To = email,
                    Subject = $"{content.TitleAr} | {content.TitleEn}",
                    Body = body,
                    QueuedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to queue {Type} notification emails.", content.Type);
        }
    }

    private async Task SendPushAsync(List<int> recipients, NotificationContent content, CancellationToken cancellationToken)
    {
        try
        {
            var mutedUserIds = await _db.UserNotificationPreferences.AsNoTracking()
                .Where(p => recipients.Contains(p.UserId) && !p.PushEnabled)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);

            var tokens = await _db.UserDeviceTokens.AsNoTracking()
                .Where(t => recipients.Contains(t.UserId) && t.IsActive && !mutedUserIds.Contains(t.UserId))
                .Select(t => t.Token)
                .ToListAsync(cancellationToken);
            if (tokens.Count == 0)
                return;

            var data = new Dictionary<string, object>
            {
                ["type"] = content.Type,
                ["titleEn"] = content.TitleEn,
                ["bodyEn"] = content.BodyEn
            };
            if (content.Data != null)
            {
                foreach (var (key, value) in content.Data)
                {
                    if (value != null)
                        data[key] = value;
                }
            }

            foreach (var token in tokens)
            {
                try
                {
                    await _push.SendPushNotificationAsync(token, content.TitleAr, content.BodyAr, SendingStrategy.Queued, data);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to queue {Type} push notification.", content.Type);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve push recipients for {Type} notification.", content.Type);
        }
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
