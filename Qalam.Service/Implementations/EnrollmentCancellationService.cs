using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Service.Implementations;

public class EnrollmentCancellationService : IEnrollmentCancellationService
{
    private readonly ApplicationDBContext _db;
    private readonly IRefundService _refundService;

    public EnrollmentCancellationService(
        ApplicationDBContext db,
        IRefundService refundService)
    {
        _db = db;
        _refundService = refundService;
    }

    public async Task CancelAsync(
        int enrollmentId,
        int cancelledByUserId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Participants)
            .Include(e => e.CourseSchedules)
                .ThenInclude(cs => cs.Attendances)
            .Include(e => e.EnrollmentRequest)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken)
            ?? throw new InvalidOperationException("Enrollment not found.");

        if (!EnrollmentLifecycleRules.CanStudentCancel(enrollment, isOwner: true))
        {
            // Caller already checked ownership; gate is status + session-started.
            if (enrollment.EnrollmentStatus == EnrollmentStatus.Active
                && EnrollmentLifecycleRules.HasSessionStarted(enrollment))
                throw new InvalidOperationException(
                    "Cannot cancel after the first session has started.");
            throw new InvalidOperationException(
                "Only pending-payment or active (before first session) enrollments can be cancelled.");
        }

        var hadSucceededPayment = enrollment.PaidByUserId.HasValue
            || enrollment.Participants.Any(p => p.PaymentStatus == PaymentStatus.Succeeded);

        if (hadSucceededPayment)
        {
            await _refundService.RefundEnrollmentPaymentsAsync(
                enrollmentId,
                reason ?? "Student cancelled before first session",
                cancelledByUserId,
                cancellationToken);
        }

        enrollment.EnrollmentStatus = EnrollmentStatus.Cancelled;
        enrollment.CancelledAt = DateTime.UtcNow;
        enrollment.CancelledByUserId = cancelledByUserId;

        foreach (var participant in enrollment.Participants)
        {
            if (participant.PaymentStatus == PaymentStatus.Pending)
                participant.PaymentStatus = PaymentStatus.Cancelled;
            else if (participant.PaymentStatus == PaymentStatus.Succeeded && hadSucceededPayment)
                participant.PaymentStatus = PaymentStatus.Refunded;
        }

        foreach (var schedule in enrollment.CourseSchedules
                     .Where(s => s.Status is ScheduleStatus.Scheduled or ScheduleStatus.InProgress))
        {
            schedule.Status = ScheduleStatus.Cancelled;
        }

        if (enrollment.EnrollmentRequest != null
            && enrollment.EnrollmentRequest.Status is RequestStatus.Pending or RequestStatus.Approved)
        {
            enrollment.EnrollmentRequest.Status = RequestStatus.Cancelled;
        }

        if (enrollment.IsFreeTrial)
        {
            var studentIds = enrollment.Participants.Select(p => p.StudentId).Distinct().ToList();
            var students = await _db.Students
                .Where(s => studentIds.Contains(s.Id) && s.HasUsedFreeTrialSession)
                .ToListAsync(cancellationToken);
            foreach (var student in students)
                student.HasUsedFreeTrialSession = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
