using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts;

public interface ITeacherFinanceImpactService
{
    Task RecordVoidedEarningDeductionAsync(
        int teacherId,
        decimal amount,
        string currency,
        string reasonCode,
        string reasonText,
        int? earningLineId,
        int? complaintId,
        int? scheduleId,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    Task RecordWarningAsync(
        int teacherId,
        int scheduleId,
        string? notes,
        int? complaintId,
        string? resolutionCode,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    Task RecordEarningDeductionPenaltyAsync(
        int teacherId,
        decimal amount,
        string currency,
        int scheduleId,
        int? complaintId,
        string? resolutionCode,
        string? notes,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    Task RecordSettlementForAlreadyPaidAsync(
        int teacherId,
        decimal amount,
        string currency,
        int refundId,
        int? complaintId,
        int? earningLineId,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    Task<bool> IsAlreadyPaidForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);
}
