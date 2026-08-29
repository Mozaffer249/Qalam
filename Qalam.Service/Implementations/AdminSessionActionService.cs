using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AdminSessionActionService : IAdminSessionActionService
{
    private readonly ICourseScheduleRepository _schedules;
    private readonly ISessionAuditService _audit;
    private readonly ISessionComplaintService _complaints;
    private readonly IRefundService _refundService;
    private readonly ITeacherManagementService _teacherManagement;

    public AdminSessionActionService(
        ICourseScheduleRepository schedules,
        ISessionAuditService audit,
        ISessionComplaintService complaints,
        IRefundService refundService,
        ITeacherManagementService teacherManagement)
    {
        _schedules = schedules;
        _audit = audit;
        _complaints = complaints;
        _refundService = refundService;
        _teacherManagement = teacherManagement;
    }

    public async Task SetAttendanceAsync(
        int scheduleId,
        int adminUserId,
        AdminSetSessionAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var schedule = await LoadScheduleAsync(scheduleId, cancellationToken);

        if (request.TeacherStatus.HasValue)
            schedule.TeacherAttendanceStatus = request.TeacherStatus.Value;

        foreach (var item in request.Students)
        {
            if (!schedule.Enrollment.Participants.Any(p => p.StudentId == item.StudentId))
                throw new InvalidOperationException($"Student {item.StudentId} is not enrolled.");

            var att = schedule.Attendances.FirstOrDefault(a => a.StudentId == item.StudentId);
            if (att == null)
            {
                att = new SessionAttendance
                {
                    CourseScheduleId = schedule.Id,
                    StudentId = item.StudentId,
                    CreatedAt = DateTime.UtcNow,
                };
                schedule.Attendances.Add(att);
            }
            att.Status = item.Status;
        }

        await _schedules.SaveChangesAsync();
        await _audit.LogAsync(
            scheduleId,
            adminUserId,
            "Admin",
            SessionAuditActionType.AttendanceSet,
            request,
            cancellationToken);
    }

    public async Task CancelAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default)
    {
        var schedule = await LoadScheduleAsync(scheduleId, cancellationToken);
        if (schedule.Status is ScheduleStatus.Completed or ScheduleStatus.Cancelled or ScheduleStatus.Rescheduled)
            throw new InvalidOperationException($"Cannot cancel session in status {schedule.Status}.");

        schedule.Status = ScheduleStatus.Cancelled;
        await _schedules.SaveChangesAsync();
        await _audit.LogAsync(
            scheduleId,
            adminUserId,
            "Admin",
            SessionAuditActionType.SessionCancelled,
            null,
            cancellationToken);
    }

    public async Task IssueRefundAsync(
        int scheduleId,
        int adminUserId,
        AdminSessionRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        var enrollmentId = await _schedules.GetEnrollmentIdByScheduleIdAsync(scheduleId, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        await _refundService.IssueRefundAsync(
            request.PaymentId,
            enrollmentId,
            request.Amount,
            "SAR",
            request.Reason,
            adminUserId,
            cancellationToken);

        await _audit.LogAsync(
            scheduleId,
            adminUserId,
            "Admin",
            SessionAuditActionType.RefundIssued,
            request,
            cancellationToken);
    }

    public async Task HoldEarningAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default)
    {
        await _complaints.HoldEarningForScheduleAsync(scheduleId, cancellationToken);
        await _audit.LogAsync(scheduleId, adminUserId, "Admin", SessionAuditActionType.EarningHeld, null, cancellationToken);
    }

    public async Task ReleaseEarningAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default)
    {
        await _complaints.ReleaseEarningForScheduleAsync(scheduleId, cancellationToken);
        await _audit.LogAsync(scheduleId, adminUserId, "Admin", SessionAuditActionType.EarningReleased, null, cancellationToken);
    }

    public async Task VoidEarningAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default)
    {
        await _complaints.VoidEarningForScheduleAsync(scheduleId, cancellationToken);
        await _audit.LogAsync(scheduleId, adminUserId, "Admin", SessionAuditActionType.EarningVoided, null, cancellationToken);
    }

    public async Task WarnTeacherAsync(
        int scheduleId,
        int adminUserId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await _audit.LogAsync(
            scheduleId,
            adminUserId,
            "Admin",
            SessionAuditActionType.TeacherWarned,
            new { notes },
            cancellationToken);
    }

    public async Task BlockTeacherAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default)
    {
        var schedule = await _schedules.GetWithParticipantsForComplaintAsync(scheduleId, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        var teacherId = schedule.Enrollment.ApprovedByTeacherId > 0
            ? schedule.Enrollment.ApprovedByTeacherId
            : schedule.Enrollment.Course?.TeacherId ?? 0;
        if (teacherId <= 0)
            throw new InvalidOperationException("Teacher not found for session.");

        await _teacherManagement.ToggleBlockTeacherAsync(teacherId, adminUserId, "Blocked from session admin action");
        await _audit.LogAsync(
            scheduleId,
            adminUserId,
            "Admin",
            SessionAuditActionType.TeacherBlocked,
            new { teacherId },
            cancellationToken);
    }

    private async Task<CourseSchedule> LoadScheduleAsync(int scheduleId, CancellationToken cancellationToken) =>
        await _schedules.GetByIdForAdminActionAsync(scheduleId, cancellationToken)
        ?? throw new InvalidOperationException("Session not found.");
}
