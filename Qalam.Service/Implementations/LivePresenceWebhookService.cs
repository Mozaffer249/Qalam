using Livekit.Server.Sdk.Dotnet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class LivePresenceWebhookService : ILivePresenceWebhookService
{
    private readonly LiveSessionSettings _settings;
    private readonly SessionSettings _sessionSettings;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly ApplicationDBContext _db;
    private readonly ILogger<LivePresenceWebhookService> _logger;

    public LivePresenceWebhookService(
        IOptions<LiveSessionSettings> settings,
        IOptions<SessionSettings> sessionSettings,
        ICourseScheduleRepository scheduleRepository,
        ApplicationDBContext db,
        ILogger<LivePresenceWebhookService> logger)
    {
        _settings = settings.Value;
        _sessionSettings = sessionSettings.Value;
        _scheduleRepository = scheduleRepository;
        _db = db;
        _logger = logger;
    }

    public async Task<(bool Ok, int StatusCode, string Message)> HandleLiveKitAsync(
        string rawBody,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return (false, 400, "Empty webhook body.");

        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return (false, 401, "Missing Authorization header.");

        var lk = _settings.LiveKit;
        if (string.IsNullOrWhiteSpace(lk.ApiKey) || string.IsNullOrWhiteSpace(lk.ApiSecret))
        {
            _logger.LogError("LiveKit webhook received but LiveSession:LiveKit ApiKey/ApiSecret are not configured.");
            return (false, 503, "LiveKit is not configured.");
        }

        WebhookEvent webhookEvent;
        try
        {
            var receiver = new WebhookReceiver(lk.ApiKey, lk.ApiSecret);
            webhookEvent = receiver.Receive(rawBody, authorizationHeader);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "LiveKit webhook verification failed. Usual cause: Signing API key in LiveKit Cloud does not match LIVEKIT_API_KEY / LIVEKIT_API_SECRET in the API container.");
            return (false, 401, "Invalid webhook signature.");
        }

        var eventName = webhookEvent.Event ?? string.Empty;
        var roomName = webhookEvent.Room?.Name;
        var identity = webhookEvent.Participant?.Identity;
        var eventId = webhookEvent.Id;

        _logger.LogInformation(
            "LiveKit webhook verified: event={EventName}, eventId={EventId}, room={Room}, identity={Identity}",
            eventName,
            eventId,
            roomName,
            identity);

        if (eventName is not ("participant_joined" or "participant_left"))
        {
            _logger.LogInformation(
                "LiveKit webhook ignored (unsupported event type): event={EventName}, eventId={EventId}",
                eventName,
                eventId);
            return (true, 200, "Ignored.");
        }

        if (string.IsNullOrWhiteSpace(eventId))
        {
            _logger.LogWarning("LiveKit webhook missing event id (event={EventName}).", eventName);
            return (false, 400, "Missing webhook event id.");
        }

        if (await _db.SessionLivePresenceEvents.AnyAsync(e => e.LiveKitEventId == eventId, cancellationToken))
        {
            _logger.LogInformation(
                "LiveKit webhook duplicate: eventId={EventId}, room={Room}, identity={Identity}",
                eventId,
                roomName,
                identity);
            return (true, 200, "Duplicate.");
        }

        if (!LiveSessionRoomNames.TryParseScheduleId(roomName, out var scheduleId))
        {
            _logger.LogInformation(
                "LiveKit webhook ignored (non-Qalam room): eventId={EventId}, room={Room}",
                eventId,
                roomName);
            return (true, 200, "Ignored room.");
        }

        if (!TryParseIdentity(identity, out var parsed))
        {
            _logger.LogInformation(
                "LiveKit webhook ignored (identity): eventId={EventId}, identity={Identity}, scheduleId={ScheduleId}",
                eventId,
                identity,
                scheduleId);
            return (true, 200, "Ignored identity.");
        }

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(scheduleId, cancellationToken);
        if (schedule == null)
        {
            _logger.LogWarning(
                "LiveKit webhook unknown schedule: eventId={EventId}, scheduleId={ScheduleId}, identity={Identity}",
                eventId,
                scheduleId,
                identity);
            return (true, 200, "Unknown schedule.");
        }

        if (schedule.Status is ScheduleStatus.Completed or ScheduleStatus.Cancelled or ScheduleStatus.Rescheduled)
        {
            _logger.LogInformation(
                "LiveKit webhook session closed: eventId={EventId}, scheduleId={ScheduleId}, status={Status}",
                eventId,
                scheduleId,
                schedule.Status);
            return (true, 200, "Session closed.");
        }

        if (!ParticipantBelongsToSchedule(schedule, parsed))
        {
            _logger.LogWarning(
                "LiveKit identity {Identity} does not belong to schedule {ScheduleId} (eventId={EventId}).",
                identity,
                scheduleId,
                eventId);
            return (true, 200, "Participant mismatch.");
        }

        var eventType = eventName == "participant_joined"
            ? LivePresenceEventType.Joined
            : LivePresenceEventType.Left;

        var occurredAt = webhookEvent.CreatedAt > 0
            ? DateTimeOffset.FromUnixTimeSeconds(webhookEvent.CreatedAt).UtcDateTime
            : DateTime.UtcNow;

        _db.SessionLivePresenceEvents.Add(new SessionLivePresenceEvent
        {
            CourseScheduleId = scheduleId,
            Role = parsed.Role,
            ParticipantId = parsed.ParticipantId,
            EventType = eventType,
            OccurredAt = occurredAt,
            LiveKitEventId = eventId,
            Identity = identity!,
        });

        if (parsed.Role == LivePresenceRole.Teacher)
            ApplyTeacherPresence(schedule, eventType, occurredAt, _sessionSettings.LateGraceMinutes);
        else
            ApplyStudentPresence(
                schedule, parsed.ParticipantId, eventType, occurredAt, _sessionSettings.LateGraceMinutes);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "LiveKit {EventType} recorded for {Role} {ParticipantId} on schedule {ScheduleId} (event {EventId}, room={Room}).",
            eventType,
            parsed.Role,
            parsed.ParticipantId,
            scheduleId,
            eventId,
            roomName);

        return (true, 200, "Processed.");
    }

    private static void ApplyTeacherPresence(
        CourseSchedule schedule,
        LivePresenceEventType eventType,
        DateTime occurredAt,
        int lateGraceMinutes)
    {
        if (eventType == LivePresenceEventType.Joined)
        {
            var startUtc = SessionAttendanceRules.ResolveStartUtc(schedule);
            SessionAttendanceRules.ApplyTeacherJoin(schedule, occurredAt, startUtc, lateGraceMinutes);
            schedule.TeacherInRoom = true;
            schedule.TeacherLeftAt = null;

            if (schedule.Status == ScheduleStatus.Scheduled)
            {
                schedule.Status = ScheduleStatus.InProgress;
                schedule.StartedAt ??= occurredAt;
            }

            return;
        }

        // Leave does not change attendance status (webhook may fail; Join remains authoritative).
        schedule.TeacherInRoom = false;
        schedule.TeacherLeftAt = occurredAt;
    }

    private static void ApplyStudentPresence(
        CourseSchedule schedule,
        int studentId,
        LivePresenceEventType eventType,
        DateTime occurredAt,
        int lateGraceMinutes)
    {
        if (eventType != LivePresenceEventType.Joined)
            return;

        var startUtc = SessionAttendanceRules.ResolveStartUtc(schedule);
        var existing = schedule.Attendances.FirstOrDefault(a => a.StudentId == studentId);
        if (existing != null)
        {
            SessionAttendanceRules.ApplyStudentJoin(existing, occurredAt, startUtc, lateGraceMinutes);
            return;
        }

        var attendance = new SessionAttendance
        {
            CourseScheduleId = schedule.Id,
            StudentId = studentId,
        };
        SessionAttendanceRules.ApplyStudentJoin(attendance, occurredAt, startUtc, lateGraceMinutes);
        schedule.Attendances.Add(attendance);
    }

    private static bool ParticipantBelongsToSchedule(CourseSchedule schedule, ParsedLiveIdentity parsed)
    {
        if (parsed.Role == LivePresenceRole.Teacher)
        {
            if (schedule.Enrollment.ApprovedByTeacherId == parsed.ParticipantId)
                return true;

            return schedule.Enrollment.Course != null
                   && schedule.Enrollment.Course.TeacherId == parsed.ParticipantId;
        }

        return schedule.Enrollment.Participants.Any(p => p.StudentId == parsed.ParticipantId);
    }

    internal static bool TryParseIdentity(string? identity, out ParsedLiveIdentity parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(identity))
            return false;

        const string teacherPrefix = "teacher-";
        const string studentPrefix = "student-";

        if (identity.StartsWith(teacherPrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(identity.AsSpan(teacherPrefix.Length), out var teacherId)
            && teacherId > 0)
        {
            parsed = new ParsedLiveIdentity(LivePresenceRole.Teacher, teacherId);
            return true;
        }

        if (identity.StartsWith(studentPrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(identity.AsSpan(studentPrefix.Length), out var studentId)
            && studentId > 0)
        {
            parsed = new ParsedLiveIdentity(LivePresenceRole.Student, studentId);
            return true;
        }

        return false;
    }
}
