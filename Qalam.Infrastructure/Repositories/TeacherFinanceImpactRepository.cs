using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class TeacherFinanceImpactRepository : ITeacherFinanceImpactRepository
{
    private readonly ApplicationDBContext _context;

    public TeacherFinanceImpactRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public Task<TeacherEarningLine?> GetEarningLineForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default) =>
        _context.TeacherEarningLines
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.CourseScheduleId == courseScheduleId, cancellationToken);

    public async Task<bool> HasPaidEarningForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TeacherEarningLines
            .AsNoTracking()
            .AnyAsync(l =>
                l.EnrollmentId == enrollmentId
                && l.Status == TeacherEarningLineStatus.IncludedInPayout
                && l.PayoutItem != null
                && l.PayoutItem.PayoutBatch.Status == PayoutBatchStatus.Paid,
                cancellationToken);
    }

    public async Task AddAdjustmentAsync(
        TeacherBalanceAdjustment adjustment,
        CancellationToken cancellationToken = default)
    {
        await _context.TeacherBalanceAdjustments.AddAsync(adjustment, cancellationToken);
    }

    public async Task AddDisciplinaryRecordAsync(
        TeacherDisciplinaryRecord record,
        CancellationToken cancellationToken = default)
    {
        await _context.TeacherDisciplinaryRecords.AddAsync(record, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
