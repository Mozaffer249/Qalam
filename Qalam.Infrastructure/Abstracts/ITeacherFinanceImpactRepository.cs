using Qalam.Data.Entity.Payment;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherFinanceImpactRepository
{
    Task<TeacherEarningLine?> GetEarningLineForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPaidEarningForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task AddAdjustmentAsync(
        TeacherBalanceAdjustment adjustment,
        CancellationToken cancellationToken = default);

    Task AddDisciplinaryRecordAsync(
        TeacherDisciplinaryRecord record,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
