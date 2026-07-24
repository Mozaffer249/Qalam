using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

/// <summary>
/// Shared complete + auto-attendance logic used by the teacher Complete command and the background sweeper.
/// </summary>
public class SessionLifecycleHelper : ISessionLifecycleService
{
    private readonly ICourseScheduleRepository _courseScheduleRepository;
    private readonly SessionSettings _settings;
    private readonly ILogger<SessionLifecycleHelper> _logger;

    public SessionLifecycleHelper(
        ICourseScheduleRepository courseScheduleRepository,
        IOptions<SessionSettings> settings,
        ILogger<SessionLifecycleHelper> logger)
    {
        _courseScheduleRepository = courseScheduleRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task CompleteByIdAsync(int courseScheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _courseScheduleRepository.GetByIdForLifecycleAsync(courseScheduleId, cancellationToken);
        if (schedule == null)
            throw new InvalidOperationException($"CourseSchedule {courseScheduleId} not found.");

        await CompleteAsync(schedule, cancellationToken);
    }

    public async Task CompleteAsync(CourseSchedule schedule, CancellationToken cancellationToken = default)
    {
        if (schedule.Status == ScheduleStatus.Completed)
            return;

        if (schedule.Status is ScheduleStatus.Cancelled or ScheduleStatus.Rescheduled)
            throw new InvalidOperationException(
                $"Cannot complete CourseSchedule {schedule.Id} in status {schedule.Status}.");

        schedule.Status = ScheduleStatus.Completed;
        schedule.EndedAt = DateTime.UtcNow;

        var autoStatus = ResolveAutoAttendanceStatus();
        AutoResolveMissingAttendance(schedule, autoStatus);

        await _courseScheduleRepository.SaveChangesAsync();
        _logger.LogInformation(
            "Completed CourseSchedule {ScheduleId}; auto-attendance default={Status}.",
            schedule.Id, autoStatus);
    }

    private SessionAttendanceStatus ResolveAutoAttendanceStatus()
    {
        var value = _settings.DefaultAutoAttendanceStatus;
        if (Enum.IsDefined(typeof(SessionAttendanceStatus), value)
            && (SessionAttendanceStatus)value is SessionAttendanceStatus.Present
                or SessionAttendanceStatus.Absent)
        {
            return (SessionAttendanceStatus)value;
        }

        return SessionAttendanceStatus.Absent;
    }

    private static void AutoResolveMissingAttendance(CourseSchedule schedule, SessionAttendanceStatus autoStatus)
    {
        if (schedule.TeacherAttendanceStatus == SessionAttendanceStatus.Pending)
        {
            schedule.TeacherAttendanceStatus = autoStatus;
        }

        var participants = schedule.Enrollment?.Participants;
        if (participants == null || participants.Count == 0)
            return;

        var byStudent = schedule.Attendances
            .GroupBy(a => a.StudentId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var participant in participants)
        {
            if (byStudent.TryGetValue(participant.StudentId, out var existing))
            {
                // Still-pending rows left unmarked by the teacher get the auto default.
                if (existing.Status == SessionAttendanceStatus.Pending)
                {
                    existing.Status = autoStatus;
                    existing.IsAutoResolved = true;
                }

                continue;
            }

            schedule.Attendances.Add(new SessionAttendance
            {
                CourseScheduleId = schedule.Id,
                StudentId = participant.StudentId,
                Status = autoStatus,
                IsAutoResolved = true,
            });
        }
    }
}
