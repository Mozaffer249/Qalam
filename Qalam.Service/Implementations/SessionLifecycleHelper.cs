using Microsoft.Extensions.Logging;
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
    private readonly ILiveSessionProvider _liveSessionProvider;
    private readonly ILogger<SessionLifecycleHelper> _logger;

    public SessionLifecycleHelper(
        ICourseScheduleRepository courseScheduleRepository,
        ILiveSessionProvider liveSessionProvider,
        ILogger<SessionLifecycleHelper> logger)
    {
        _courseScheduleRepository = courseScheduleRepository;
        _liveSessionProvider = liveSessionProvider;
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

        // Never invent Present for never-joined; Pending + no JoinedAt → Absent.
        SessionAttendanceRules.AutoResolveMissingAttendance(schedule);

        await _courseScheduleRepository.SaveChangesAsync();
        _logger.LogInformation(
            "Completed CourseSchedule {ScheduleId}; auto-attendance default=Absent for never-joined.",
            schedule.Id);

        // Close the LiveKit room so connected clients disconnect (soft-fail inside provider).
        var roomName = LiveSessionRoomNames.ForSchedule(schedule.Id);
        await _liveSessionProvider.EndRoomAsync(roomName, cancellationToken);
    }

    public async Task MarkInProgressAsync(CourseSchedule schedule, CancellationToken cancellationToken = default)
    {
        if (schedule.Status is ScheduleStatus.InProgress
            or ScheduleStatus.Completed
            or ScheduleStatus.Cancelled
            or ScheduleStatus.Rescheduled)
            return;

        if (schedule.Status != ScheduleStatus.Scheduled)
            return;

        schedule.Status = ScheduleStatus.InProgress;
        await _courseScheduleRepository.SaveChangesAsync();
        _logger.LogInformation("Marked CourseSchedule {ScheduleId} InProgress (auto-start).", schedule.Id);
    }
}
