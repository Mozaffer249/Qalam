using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class SessionPresenceService : ISessionPresenceService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly ApplicationDBContext _db;
    private readonly SessionSettings _sessionSettings;
    private readonly ILogger<SessionPresenceService> _logger;

    public SessionPresenceService(
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        ICourseScheduleRepository scheduleRepository,
        ApplicationDBContext db,
        IOptions<SessionSettings> sessionSettings,
        ILogger<SessionPresenceService> logger)
    {
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _scheduleRepository = scheduleRepository;
        _db = db;
        _sessionSettings = sessionSettings.Value;
        _logger = logger;
    }

    public async Task<(bool Ok, string Message, bool Forbidden, bool NotFound)> JoinAsTeacherAsync(
        int userId,
        int courseScheduleId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return (false, "Teacher profile not found.", false, true);

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(courseScheduleId, cancellationToken);
        if (schedule == null)
            return (false, "Session not found.", false, true);

        if (!TeacherOwnsSchedule(schedule, teacher.Id))
            return (false, "This session does not belong to you.", true, false);

        var gate = ValidateJoinWindow(schedule);
        if (gate != null)
            return (false, gate, false, false);

        var now = DateTime.UtcNow;
        schedule.TeacherAttendanceStatus = SessionAttendanceStatus.Present;
        schedule.TeacherJoinedAt ??= now;
        schedule.TeacherInRoom = true;
        schedule.TeacherLeftAt = null;

        if (schedule.Status == ScheduleStatus.Scheduled)
        {
            schedule.Status = ScheduleStatus.InProgress;
            schedule.StartedAt ??= now;
        }

        AddApiPresenceEvent(
            schedule.Id,
            LivePresenceRole.Teacher,
            teacher.Id,
            LivePresenceEventType.Joined,
            now,
            $"teacher-{teacher.Id}");

        await _scheduleRepository.SaveChangesAsync();
        _logger.LogInformation(
            "Teacher {TeacherId} joined CourseSchedule {ScheduleId}.",
            teacher.Id, schedule.Id);

        return (true, "Joined session.", false, false);
    }

    public async Task<(bool Ok, string Message, bool Forbidden, bool NotFound)> LeaveAsTeacherAsync(
        int userId,
        int courseScheduleId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return (false, "Teacher profile not found.", false, true);

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(courseScheduleId, cancellationToken);
        if (schedule == null)
            return (false, "Session not found.", false, true);

        if (!TeacherOwnsSchedule(schedule, teacher.Id))
            return (false, "This session does not belong to you.", true, false);

        if (schedule.Status is ScheduleStatus.Cancelled or ScheduleStatus.Rescheduled)
            return (false, $"Cannot leave a session in status {schedule.Status}.", false, false);

        var now = DateTime.UtcNow;
        schedule.TeacherInRoom = false;
        schedule.TeacherLeftAt = now;

        AddApiPresenceEvent(
            schedule.Id,
            LivePresenceRole.Teacher,
            teacher.Id,
            LivePresenceEventType.Left,
            now,
            $"teacher-{teacher.Id}");

        await _scheduleRepository.SaveChangesAsync();
        _logger.LogInformation(
            "Teacher {TeacherId} left CourseSchedule {ScheduleId}.",
            teacher.Id, schedule.Id);

        return (true, "Left session.", false, false);
    }

    public async Task<(bool Ok, string Message, bool Forbidden, bool NotFound)> JoinAsStudentAsync(
        int userId,
        int courseScheduleId,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByUserIdAsync(userId);
        if (student == null)
            return (false, "Student profile not found.", false, true);

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(courseScheduleId, cancellationToken);
        if (schedule == null)
            return (false, "Session not found.", false, true);

        var isParticipant = schedule.Enrollment.Participants.Any(p => p.StudentId == student.Id);
        if (!isParticipant)
            return (false, "You are not a participant in this enrollment.", true, false);

        var gate = ValidateJoinWindow(schedule);
        if (gate != null)
            return (false, gate, false, false);

        var now = DateTime.UtcNow;
        var existing = schedule.Attendances.FirstOrDefault(a => a.StudentId == student.Id);
        if (existing != null)
        {
            existing.Status = SessionAttendanceStatus.Present;
            existing.JoinedAt ??= now;
            existing.IsAutoResolved = false;
        }
        else
        {
            schedule.Attendances.Add(new SessionAttendance
            {
                CourseScheduleId = schedule.Id,
                StudentId = student.Id,
                Status = SessionAttendanceStatus.Present,
                JoinedAt = now,
                IsAutoResolved = false,
            });
        }

        AddApiPresenceEvent(
            schedule.Id,
            LivePresenceRole.Student,
            student.Id,
            LivePresenceEventType.Joined,
            now,
            $"student-{student.Id}");

        await _scheduleRepository.SaveChangesAsync();
        _logger.LogInformation(
            "Student {StudentId} joined CourseSchedule {ScheduleId}.",
            student.Id, schedule.Id);

        return (true, "Joined session.", false, false);
    }

    private void AddApiPresenceEvent(
        int scheduleId,
        LivePresenceRole role,
        int participantId,
        LivePresenceEventType eventType,
        DateTime occurredAt,
        string identity)
    {
        _db.SessionLivePresenceEvents.Add(new SessionLivePresenceEvent
        {
            CourseScheduleId = scheduleId,
            Role = role,
            ParticipantId = participantId,
            EventType = eventType,
            OccurredAt = occurredAt,
            // Unique synthetic id so API join/leave works without LiveKit webhooks.
            LiveKitEventId = $"api-{eventType.ToString().ToLowerInvariant()}-{role.ToString().ToLowerInvariant()}-{scheduleId}-{participantId}-{Guid.NewGuid():N}",
            Identity = identity,
        });
    }

    private string? ValidateJoinWindow(CourseSchedule schedule)
    {
        if (schedule.Status is ScheduleStatus.Completed or ScheduleStatus.Cancelled or ScheduleStatus.Rescheduled)
            return $"Cannot join a session in status {schedule.Status}.";

        var slot = schedule.TeacherAvailability?.TimeSlot;
        if (slot == null)
            return "Session time slot is not available.";

        if (schedule.Enrollment?.EnrollmentStatus != EnrollmentStatus.Active)
            return "Enrollment is not active.";

        if (schedule.Status is not (ScheduleStatus.Scheduled or ScheduleStatus.InProgress))
            return $"Cannot join a session in status {schedule.Status}.";

        if (!_sessionSettings.EnforceJoinWindow)
            return null;

        var utcNow = DateTime.UtcNow;
        var startUtc = schedule.Date.ToDateTime(TimeOnly.FromTimeSpan(slot.StartTime), DateTimeKind.Utc);
        var endUtc = schedule.Date.ToDateTime(TimeOnly.FromTimeSpan(slot.EndTime), DateTimeKind.Utc);

        if (utcNow < startUtc)
            return "Cannot join before the session start time.";

        if (utcNow > endUtc)
            return "Cannot join after the session end time.";

        return null;
    }

    private static bool TeacherOwnsSchedule(CourseSchedule schedule, int teacherId)
    {
        if (schedule.Enrollment == null)
            return false;

        if (schedule.Enrollment.ApprovedByTeacherId == teacherId)
            return true;

        return schedule.Enrollment.Course != null
               && schedule.Enrollment.Course.TeacherId == teacherId;
    }
}
