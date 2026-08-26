using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class EnrollmentApprovalService : IEnrollmentApprovalService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ApplicationDBContext _db;
    private readonly IFreeSessionPolicyService _freeSessionPolicy;

    public EnrollmentApprovalService(
        IEnrollmentRepository enrollmentRepository,
        ApplicationDBContext db,
        IFreeSessionPolicyService freeSessionPolicy)
    {
        _enrollmentRepository = enrollmentRepository;
        _db = db;
        _freeSessionPolicy = freeSessionPolicy;
    }

    public async Task<Enrollment> CreatePendingPaymentArtifactsAsync(
        CourseEnrollmentRequest request,
        Course course,
        int approvingTeacherId,
        DateTime paymentDeadline,
        CancellationToken cancellationToken)
    {
        var confirmedMembers = request.GroupMembers
            .Where(gm => gm.ConfirmationStatus == GroupMemberConfirmationStatus.Confirmed)
            .ToList();

        if (confirmedMembers.Count == 0)
            throw new InvalidOperationException(
                $"No confirmed group members; cannot create enrollment artifacts for request {request.Id}.");

        var isGroupCourse = string.Equals(course.SessionType?.Code, "group", StringComparison.OrdinalIgnoreCase);

        var kind = isGroupCourse ? EnrollmentKind.Group : EnrollmentKind.Individual;
        int? leaderStudentId = null;
        var primaryStudentId = confirmedMembers
            .FirstOrDefault(gm => gm.MemberType == GroupMemberType.Own)?.StudentId
            ?? confirmedMembers.First().StudentId;

        if (isGroupCourse)
            leaderStudentId = primaryStudentId;

        var sessionCount = course.SessionsCount
            ?? course.Sessions?.Count
            ?? 0;
        if (sessionCount <= 0 && course.Sessions != null)
            sessionCount = course.Sessions.Count;
        if (sessionCount <= 0)
            sessionCount = 1;

        var firstSessionMinutes = course.Sessions?
            .OrderBy(s => s.SessionNumber)
            .Select(s => s.DurationMinutes)
            .FirstOrDefault() ?? 0;
        if (firstSessionMinutes <= 0)
            firstSessionMinutes = course.SessionDurationMinutes ?? 60;

        var gross = request.EstimatedTotalPrice;
        var applyFreeTrial = _freeSessionPolicy.IsEligiblePackage(isGroupCourse, sessionCount)
            && await _freeSessionPolicy.IsStudentEligibleForFreeTrialAsync(primaryStudentId, cancellationToken);

        var amountDue = gross;
        if (applyFreeTrial && request.PricingSnapshotId.HasValue)
        {
            var snapshot = await _db.PricingSnapshots
                .FirstOrDefaultAsync(s => s.Id == request.PricingSnapshotId.Value, cancellationToken);
            if (snapshot != null)
            {
                var packageTotal = snapshot.TotalPrice > 0 ? snapshot.TotalPrice : gross;
                (amountDue, _) = FreeSessionPolicyService.ApplyFreeTrialToSnapshot(
                    snapshot, packageTotal, firstSessionMinutes);
            }
            else
            {
                var totalMins = course.Sessions?.Sum(s => s.DurationMinutes)
                    ?? sessionCount * firstSessionMinutes;
                var hourly = totalMins > 0
                    ? Math.Round(gross * 60m / totalMins, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                amountDue = Math.Max(
                    0m,
                    gross - FreeSessionPolicyService.ComputeFreeSessionCredit(
                        hourly, firstSessionMinutes, gross));
            }
        }
        else if (applyFreeTrial)
        {
            var totalMins = course.Sessions?.Sum(s => s.DurationMinutes)
                ?? sessionCount * firstSessionMinutes;
            var hourly = totalMins > 0
                ? Math.Round(gross * 60m / totalMins, 2, MidpointRounding.AwayFromZero)
                : 0m;
            amountDue = Math.Max(
                0m,
                gross - FreeSessionPolicyService.ComputeFreeSessionCredit(
                    hourly, firstSessionMinutes, gross));
        }

        var enrollment = new Enrollment
        {
            CourseId = request.CourseId,
            EnrollmentRequestId = request.Id,
            Kind = kind,
            LeaderStudentId = leaderStudentId,
            ApprovedByTeacherId = approvingTeacherId,
            ApprovedAt = DateTime.UtcNow,
            PaymentDeadline = amountDue <= 0 ? null : paymentDeadline,
            EnrollmentStatus = amountDue <= 0 ? EnrollmentStatus.Active : EnrollmentStatus.PendingPayment,
            ActivatedAt = amountDue <= 0 ? DateTime.UtcNow : null,
            AmountDue = amountDue,
            IsFreeTrial = applyFreeTrial,
            PricingSnapshotId = request.PricingSnapshotId,
            OwnerUserId = request.RequestedByUserId,
            PreferredStartDate = request.PreferredStartDate,
            PreferredEndDate = request.PreferredEndDate,
            Participants = confirmedMembers.Select(gm => new EnrollmentParticipant
            {
                StudentId = gm.StudentId,
                PaymentStatus = amountDue <= 0 ? PaymentStatus.Succeeded : PaymentStatus.Pending,
                PaidAt = amountDue <= 0 ? DateTime.UtcNow : null
            }).ToList()
        };

        await _enrollmentRepository.AddAsync(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        if (applyFreeTrial)
        {
            var domainId = course.TeacherSubject?.Subject?.DomainId ?? course.DomainId;
            await _freeSessionPolicy.ReserveStudentFreeTrialAsync(
                primaryStudentId,
                enrollment,
                FreeTrialConsumptionSource.CourseEnrollment,
                approvingTeacherId,
                domainId,
                cancellationToken: cancellationToken);
        }

        return enrollment;
    }
}
