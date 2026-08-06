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
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly ITeacherRepository _teacherRepo;
    private readonly IRabbitMQService _rabbitMq;
    private readonly IOsrNotificationSettingsProvider _notificationSettings;
    private readonly ILogger<OpenSessionRequestTargetingService> _logger;

    public OpenSessionRequestTargetingService(
        ITeacherMatchingService matching,
        IOpenSessionRequestTargetRepository targetRepo,
        IOpenSessionRequestRepository requestRepo,
        ITeacherRepository teacherRepo,
        IRabbitMQService rabbitMq,
        IOsrNotificationSettingsProvider notificationSettings,
        ILogger<OpenSessionRequestTargetingService> logger)
    {
        _matching = matching;
        _targetRepo = targetRepo;
        _requestRepo = requestRepo;
        _teacherRepo = teacherRepo;
        _rabbitMq = rabbitMq;
        _notificationSettings = notificationSettings;
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

        await NotifyTeachersAsync(
            newTeacherIds,
            subject: "طلب جلسات جديد مطابق لتخصصك",
            emailBody: "يوجد طلب جلسات جديد مطابق لتخصصك. افتح لوحة \"الطلبات الجديدة\" لعرض التفاصيل وتقديم عرضك.",
            smsBody: "طلب جلسات جديد مطابق لتخصصك على منصة قلم. افتح الطلبات الجديدة لتقديم عرضك.",
            now,
            cancellationToken);

        return newTargets.Count;
    }

    public async Task<int> NotifyTargetedTeacherAsync(int requestId, int teacherId, CancellationToken cancellationToken = default)
    {
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

        await NotifyTeachersAsync(
            new List<int> { teacherId },
            subject: "طلب جلسات جديد موجَّه إليك",
            emailBody: "تم إرسال طلب جلسات جديد موجَّه إليك مباشرة من الطالب. افتح لوحة \"الطلبات الجديدة\" لعرض التفاصيل وتقديم عرضك.",
            smsBody: "طلب جلسات موجَّه إليك على منصة قلم. افتح الطلبات الجديدة لتقديم عرضك.",
            now,
            cancellationToken);

        return 1;
    }

    public async Task<int> RematchTeacherForSubjectsAsync(
        int teacherId,
        IReadOnlyList<int> subjectIds,
        CancellationToken cancellationToken = default)
    {
        if (subjectIds.Count == 0) return 0;

        var distinctSubjects = subjectIds.Distinct().ToList();
        var requestIds = await _requestRepo.GetOpenBroadcastRequestIdsBySubjectIdsAsync(
            distinctSubjects, cancellationToken);
        if (requestIds.Count == 0)
        {
            _logger.LogInformation(
                "Rematch teacher {TeacherId} for subjects [{Subjects}]: no open broadcast OSRs.",
                teacherId, string.Join(",", distinctSubjects));
            return 0;
        }

        var now = DateTime.UtcNow;
        var newTargets = new List<OpenSessionRequestTarget>();

        foreach (var requestId in requestIds)
        {
            var existing = await _targetRepo.GetByRequestAndTeacherAsync(requestId, teacherId, cancellationToken);
            if (existing != null) continue;

            newTargets.Add(new OpenSessionRequestTarget
            {
                SessionRequestId = requestId,
                TeacherId = teacherId,
                MatchedAt = now,
                NotifiedAt = now,
                Status = OpenSessionRequestTargetStatus.Notified,
                CreatedAt = now
            });
        }

        if (newTargets.Count == 0)
        {
            _logger.LogInformation(
                "Rematch teacher {TeacherId}: already targeted on all matching open OSRs.",
                teacherId);
            return 0;
        }

        await _targetRepo.BulkInsertAsync(newTargets, cancellationToken);
        _logger.LogInformation(
            "Rematch teacher {TeacherId}: added to {Count} open OSRs.",
            teacherId, newTargets.Count);

        await NotifyTeachersAsync(
            new List<int> { teacherId },
            subject: "طلبات جلسات جديدة مطابقة لتخصصك",
            emailBody: $"تمت إضافة {newTargets.Count} طلب(ات) جلسات مطابقة لتخصصك الجديد. افتح لوحة \"الطلبات الجديدة\" لعرضها.",
            smsBody: $"تمت مطابقتك مع {newTargets.Count} طلب جلسات جديد على منصة قلم.",
            now,
            cancellationToken);

        return newTargets.Count;
    }

    private async Task NotifyTeachersAsync(
        IReadOnlyList<int> teacherIds,
        string subject,
        string emailBody,
        string smsBody,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var settings = await _notificationSettings.GetSettingsAsync(cancellationToken);
        if (!settings.EmailEnabled && !settings.SmsEnabled && !settings.PushEnabled)
            return;

        var contacts = await _teacherRepo.GetContactInfoByTeacherIdsAsync(teacherIds, cancellationToken);

        foreach (var (teacherId, email, phone) in contacts)
        {
            if (settings.EmailEnabled && !string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    await _rabbitMq.QueueEmailAsync(new EmailMessage
                    {
                        To = email!,
                        Subject = subject,
                        Body = emailBody,
                        QueuedAt = now
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to queue OSR email for teacher {TeacherId}.", teacherId);
                }
            }

            if (settings.SmsEnabled && !string.IsNullOrWhiteSpace(phone))
            {
                try
                {
                    await _rabbitMq.QueueSmsAsync(new SmsMessage
                    {
                        PhoneNumber = phone!,
                        Content = smsBody,
                        QueuedAt = now
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to queue OSR SMS for teacher {TeacherId}.", teacherId);
                }
            }
            else if (settings.SmsEnabled)
            {
                _logger.LogDebug("OSR SMS enabled but teacher {TeacherId} has no phone; skipped.", teacherId);
            }

            if (settings.PushEnabled)
            {
                // No device-token store yet — controlled no-op until registration lands.
                _logger.LogDebug(
                    "OSR push enabled but no device token for teacher {TeacherId}; skipped.",
                    teacherId);
            }
        }
    }
}
