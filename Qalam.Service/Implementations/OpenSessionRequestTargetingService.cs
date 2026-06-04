using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class OpenSessionRequestTargetingService : IOpenSessionRequestTargetingService
{
    private readonly ITeacherMatchingService _matching;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;
    private readonly ITeacherRepository _teacherRepo;
    private readonly IRabbitMQService _rabbitMq;
    private readonly ILogger<OpenSessionRequestTargetingService> _logger;

    public OpenSessionRequestTargetingService(
        ITeacherMatchingService matching,
        IOpenSessionRequestTargetRepository targetRepo,
        ITeacherRepository teacherRepo,
        IRabbitMQService rabbitMq,
        ILogger<OpenSessionRequestTargetingService> logger)
    {
        _matching = matching;
        _targetRepo = targetRepo;
        _teacherRepo = teacherRepo;
        _rabbitMq = rabbitMq;
        _logger = logger;
    }

    public async Task<int> RunMatchingAndNotifyAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var newTeacherIds = await _matching.FindMatchingTeacherIdsAsync(requestId, cancellationToken);
        if (newTeacherIds.Count == 0)
        {
            _logger.LogInformation("Matching for request {RequestId}: no new teachers to target.", requestId);
            return 0;
        }

        var now = DateTime.UtcNow;
        var newTargets = newTeacherIds.Select(teacherId => new OpenSessionRequestTarget
        {
            SessionRequestId = requestId,
            TeacherId = teacherId,
            MatchedAt = now,
            NotifiedAt = now,
            Status = OpenSessionRequestTargetStatus.Notified,
            CreatedAt = now
        }).ToList();

        await _targetRepo.BulkInsertAsync(newTargets, cancellationToken);
        _logger.LogInformation("Matching for request {RequestId}: targeted {Count} teachers.", requestId, newTargets.Count);

        // Email notifications. Push notifications are skipped for now — no device-token
        // infrastructure exists yet (PushNotificationMessage requires DeviceToken). Wire push
        // here when the device-token table lands.
        var emails = await _teacherRepo.GetEmailsByTeacherIdsAsync(newTeacherIds, cancellationToken);
        foreach (var (teacherId, email) in emails)
        {
            try
            {
                await _rabbitMq.QueueEmailAsync(new EmailMessage
                {
                    To = email,
                    Subject = "طلب جلسات جديد مطابق لتخصصك",
                    Body = $"يوجد طلب جلسات جديد مطابق لتخصصك. افتح لوحة \"الطلبات الجديدة\" لعرض التفاصيل وتقديم عرضك.",
                    QueuedAt = now
                });
            }
            catch (Exception ex)
            {
                // Don't fail the publishing flow if RabbitMQ is degraded — the target row is
                // already persisted, so the teacher will still see it in their inbox poll.
                _logger.LogWarning(ex, "Failed to queue match-notification email for teacher {TeacherId}.", teacherId);
            }
        }

        return newTargets.Count;
    }

    public async Task<int> NotifyTargetedTeacherAsync(int requestId, int teacherId, CancellationToken cancellationToken = default)
    {
        // Idempotent — if a Target row for this (request, teacher) already exists, do nothing.
        var existing = await _targetRepo.GetByRequestAndTeacherAsync(requestId, teacherId, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation(
                "Targeted-teacher notify for request {RequestId} → teacher {TeacherId}: already targeted, skipping.",
                requestId, teacherId);
            return 0;
        }

        var now = DateTime.UtcNow;
        await _targetRepo.BulkInsertAsync(new[]
        {
            new OpenSessionRequestTarget
            {
                SessionRequestId = requestId,
                TeacherId = teacherId,
                MatchedAt = now,
                NotifiedAt = now,
                Status = OpenSessionRequestTargetStatus.Notified,
                CreatedAt = now
            }
        }, cancellationToken);

        _logger.LogInformation(
            "Targeted-teacher notify for request {RequestId} → teacher {TeacherId}: target row created.",
            requestId, teacherId);

        // Email notification (skip push for the same reason as the broadcast path — no device tokens yet).
        var emails = await _teacherRepo.GetEmailsByTeacherIdsAsync(new List<int> { teacherId }, cancellationToken);
        var email = emails.FirstOrDefault(e => e.TeacherId == teacherId).Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            try
            {
                await _rabbitMq.QueueEmailAsync(new EmailMessage
                {
                    To = email,
                    Subject = "طلب جلسات جديد موجَّه إليك",
                    Body = "تم إرسال طلب جلسات جديد موجَّه إليك مباشرة من الطالب. افتح لوحة \"الطلبات الجديدة\" لعرض التفاصيل وتقديم عرضك.",
                    QueuedAt = now
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to queue targeted-teacher notification email for teacher {TeacherId} on request {RequestId}.",
                    teacherId, requestId);
            }
        }

        return 1;
    }
}
