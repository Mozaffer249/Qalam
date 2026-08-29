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
    private readonly IFreeSessionPolicyService _freeSessionPolicy;
    private readonly ITeacherLevelProgressionService _progressionService;
    private readonly IEnrollmentCompletionService _enrollmentCompletion;
    private readonly ITeacherEarningService _teacherEarning;
    private readonly ISessionComplaintService _sessionComplaints;
    private readonly ILogger<SessionLifecycleHelper> _logger;

    public SessionLifecycleHelper(
        ICourseScheduleRepository courseScheduleRepository,
        ILiveSessionProvider liveSessionProvider,
        IFreeSessionPolicyService freeSessionPolicy,
        ITeacherLevelProgressionService progressionService,
        IEnrollmentCompletionService enrollmentCompletion,
        ITeacherEarningService teacherEarning,
        ISessionComplaintService sessionComplaints,
        ILogger<SessionLifecycleHelper> logger)
    {
        _courseScheduleRepository = courseScheduleRepository;
        _liveSessionProvider = liveSessionProvider;
        _freeSessionPolicy = freeSessionPolicy;
        _progressionService = progressionService;
        _enrollmentCompletion = enrollmentCompletion;
        _teacherEarning = teacherEarning;
        _sessionComplaints = sessionComplaints;
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

        var teacherId = schedule.Enrollment?.ApprovedByTeacherId
            ?? schedule.Enrollment?.Course?.TeacherId;
        var domainId = ResolveDomainId(schedule.Enrollment);
        if (teacherId.HasValue && domainId > 0)
        {
            try
            {
                await _freeSessionPolicy.TryCompleteTeacherInterviewAsync(
                    teacherId.Value,
                    domainId,
                    schedule.EnrollmentId,
                    schedule.Id,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to unlock teacher interview after completing CourseSchedule {ScheduleId}",
                    schedule.Id);
            }

            try
            {
                await _progressionService.EvaluateTeacherAsync(
                    teacherId.Value, domainId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to evaluate teacher progression after completing CourseSchedule {ScheduleId}",
                    schedule.Id);
            }
        }

        if (schedule.Enrollment?.IsFreeTrial == true)
        {
            try
            {
                await _freeSessionPolicy.MarkConsumptionConsumedAsync(
                    schedule.EnrollmentId, schedule.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to mark free-trial consumption consumed for CourseSchedule {ScheduleId}",
                    schedule.Id);
            }
        }

        // Close the LiveKit room so connected clients disconnect (soft-fail inside provider).
        var roomName = LiveSessionRoomNames.ForSchedule(schedule.Id);
        await _liveSessionProvider.EndRoomAsync(roomName, cancellationToken);

        try
        {
            var holdStatus = await ResolveAccrualStatusAsync(schedule.Id, cancellationToken);
            await _teacherEarning.AccrueForCompletedScheduleAsync(
                schedule.Id,
                holdStatus,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to accrue teacher earning after completing CourseSchedule {ScheduleId}",
                schedule.Id);
        }

        try
        {
            await _enrollmentCompletion.TryCompleteEnrollmentIfFinishedAsync(
                schedule.EnrollmentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to complete enrollment after CourseSchedule {ScheduleId}",
                schedule.Id);
        }
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

    private async Task<TeacherEarningLineStatus> ResolveAccrualStatusAsync(
        int courseScheduleId,
        CancellationToken cancellationToken)
    {
        if (await _sessionComplaints.HasBlockingComplaintAsync(courseScheduleId, cancellationToken))
            return TeacherEarningLineStatus.OnHold;
        return TeacherEarningLineStatus.Pending;
    }

    private static int ResolveDomainId(Enrollment? enrollment)
    {
        if (enrollment == null)
            return 0;
        if (enrollment.Course?.TeacherSubject?.Subject?.DomainId is > 0)
            return enrollment.Course.TeacherSubject.Subject.DomainId;
        if (enrollment.PricingSnapshot?.DomainId is > 0)
            return enrollment.PricingSnapshot.DomainId;
        if (enrollment.OpenSessionRequest?.DomainId is > 0)
            return enrollment.OpenSessionRequest.DomainId;
        return 0;
    }
}
