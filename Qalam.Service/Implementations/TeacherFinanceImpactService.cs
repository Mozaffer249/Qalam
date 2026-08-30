using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherFinanceImpactService : ITeacherFinanceImpactService
{
    private readonly ITeacherFinanceImpactRepository _impact;

    public TeacherFinanceImpactService(ITeacherFinanceImpactRepository impact)
    {
        _impact = impact;
    }

    public Task<bool> IsAlreadyPaidForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default) =>
        _impact.HasPaidEarningForEnrollmentAsync(enrollmentId, cancellationToken);

    public async Task RecordVoidedEarningDeductionAsync(
        int teacherId,
        decimal amount,
        string currency,
        string reasonCode,
        string reasonText,
        int? earningLineId,
        int? complaintId,
        int? scheduleId,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return;

        await _impact.AddAdjustmentAsync(new TeacherBalanceAdjustment
        {
            TeacherId = teacherId,
            Amount = amount,
            Currency = currency,
            Kind = TeacherBalanceAdjustmentKind.Deduction,
            Status = TeacherBalanceAdjustmentStatus.Applied,
            ReasonCode = reasonCode,
            ReasonText = reasonText,
            RelatedEarningLineId = earningLineId,
            RelatedComplaintId = complaintId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        await _impact.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordWarningAsync(
        int teacherId,
        int scheduleId,
        string? notes,
        int? complaintId,
        string? resolutionCode,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        await _impact.AddDisciplinaryRecordAsync(new TeacherDisciplinaryRecord
        {
            TeacherId = teacherId,
            Kind = TeacherDisciplinaryKind.Warning,
            Amount = null,
            ComplaintId = complaintId,
            CourseScheduleId = scheduleId,
            ResolutionCode = resolutionCode,
            Notes = notes,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        await _impact.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordEarningDeductionPenaltyAsync(
        int teacherId,
        decimal amount,
        string currency,
        int scheduleId,
        int? complaintId,
        string? resolutionCode,
        string? notes,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        await _impact.AddDisciplinaryRecordAsync(new TeacherDisciplinaryRecord
        {
            TeacherId = teacherId,
            Kind = TeacherDisciplinaryKind.EarningDeduction,
            Amount = amount > 0 ? amount : null,
            Currency = currency,
            ComplaintId = complaintId,
            CourseScheduleId = scheduleId,
            ResolutionCode = resolutionCode,
            Notes = notes,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        await _impact.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordSettlementForAlreadyPaidAsync(
        int teacherId,
        decimal amount,
        string currency,
        int refundId,
        int? complaintId,
        int? earningLineId,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return;

        await _impact.AddAdjustmentAsync(new TeacherBalanceAdjustment
        {
            TeacherId = teacherId,
            Amount = amount,
            Currency = currency,
            Kind = TeacherBalanceAdjustmentKind.Settlement,
            Status = TeacherBalanceAdjustmentStatus.Applied,
            ReasonCode = "AlreadyPaidClawback",
            ReasonText = "Settlement for refund after payout",
            RelatedRefundId = refundId,
            RelatedEarningLineId = earningLineId,
            RelatedComplaintId = complaintId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        await _impact.SaveChangesAsync(cancellationToken);
    }
}
