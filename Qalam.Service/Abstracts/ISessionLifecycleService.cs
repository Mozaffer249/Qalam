using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Completes a <see cref="CourseSchedule"/> and auto-resolves missing participant attendance.
/// Also marks due schedules InProgress for the background sweeper.
/// </summary>
public interface ISessionLifecycleService
{
    /// <summary>
    /// Load schedule by id (with participants + attendances) and complete it.
    /// </summary>
    Task CompleteByIdAsync(int courseScheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete an already-tracked schedule that has Enrollment.Participants and Attendances loaded.
    /// Sets Status=Completed, EndedAt=UtcNow, and creates auto-resolved attendance for missing participants.
    /// </summary>
    /// <param name="completedByTeacher">
    /// True when the teacher completed it (they were present). Otherwise a teacher who never joined is
    /// treated as a no-show: the session is cancelled and the teacher no-show policy applies.
    /// </param>
    Task CompleteAsync(
        CourseSchedule schedule,
        CancellationToken cancellationToken = default,
        bool completedByTeacher = false);

    /// <summary>
    /// Sets Status=InProgress when still Scheduled. No-op for terminal or already InProgress.
    /// Does not set attendance or join timestamps.
    /// </summary>
    Task MarkInProgressAsync(CourseSchedule schedule, CancellationToken cancellationToken = default);
}
