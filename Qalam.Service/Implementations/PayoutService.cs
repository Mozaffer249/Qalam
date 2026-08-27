using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PayoutService : IPayoutService
{
    private readonly ApplicationDBContext _db;

    public PayoutService(ApplicationDBContext db)
    {
        _db = db;
    }

    public async Task<List<AdminPendingEarningDto>> ListPendingEarningsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.Status == TeacherEarningLineStatus.Pending)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new AdminPendingEarningDto
            {
                Id = l.Id,
                TeacherId = l.TeacherId,
                TeacherName = l.Teacher.User != null
                    ? ((l.Teacher.User.FirstName ?? "") + " " + (l.Teacher.User.LastName ?? "")).Trim()
                    : null,
                EnrollmentId = l.EnrollmentId,
                CourseTitle = l.Enrollment.Course != null
                    ? l.Enrollment.Course.Title
                    : null,
                CourseScheduleId = l.CourseScheduleId,
                Amount = l.Amount,
                Currency = l.Currency,
                Source = l.Source.ToString(),
                IsFreeTrialEnrollment = l.Enrollment.IsFreeTrial,
                FreeSessionsInEnrollment = l.Enrollment.IsFreeTrial ? 1 : 0,
                CreatedAt = l.CreatedAt
            })
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminPayoutBatchDto> CreateBatchFromPendingAsync(
        DateTime? periodStart,
        DateTime? periodEnd,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var start = periodStart ?? DateTime.UtcNow.AddMonths(-1);
        var end = periodEnd ?? DateTime.UtcNow;

        var lines = await _db.TeacherEarningLines
            .Where(l => l.Status == TeacherEarningLineStatus.Pending
                        && l.CreatedAt >= start
                        && l.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        if (lines.Count == 0)
            throw new InvalidOperationException("No pending earnings in the selected period.");

        var currency = lines.Select(l => l.Currency).FirstOrDefault() ?? "SAR";
        var byTeacher = lines.GroupBy(l => l.TeacherId).ToList();

        var batch = new PayoutBatch
        {
            PeriodStart = start,
            PeriodEnd = end,
            Currency = currency,
            Status = PayoutBatchStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = 0
        };

        foreach (var group in byTeacher)
        {
            var amount = group.Sum(l => l.Amount);
            var item = new PayoutItem
            {
                TeacherId = group.Key,
                Amount = amount,
                Currency = currency,
                CreatedAt = DateTime.UtcNow
            };
            batch.Items.Add(item);
            batch.TotalAmount += amount;

            foreach (var line in group)
            {
                line.Status = TeacherEarningLineStatus.IncludedInPayout;
                line.PayoutItem = item;
            }
        }

        _db.PayoutBatches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetBatchAsync(batch.Id, cancellationToken))!;
    }

    public async Task<AdminPayoutBatchDto?> ApproveAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _db.PayoutBatches
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status != PayoutBatchStatus.Draft)
            throw new InvalidOperationException("Only draft batches can be approved.");

        batch.Status = PayoutBatchStatus.Approved;
        batch.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> MarkPaidAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _db.PayoutBatches
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status is not PayoutBatchStatus.Approved and not PayoutBatchStatus.Draft)
            throw new InvalidOperationException("Batch cannot be marked paid in its current status.");

        batch.Status = PayoutBatchStatus.Paid;
        batch.PaidAt = DateTime.UtcNow;
        batch.ApprovedAt ??= batch.PaidAt;
        batch.MockTransferRef ??= $"MOCK-PAYOUT-{batch.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        await _db.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<List<AdminPayoutBatchListItemDto>> ListBatchesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.PayoutBatches
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new AdminPayoutBatchListItemDto
            {
                Id = b.Id,
                PeriodStart = b.PeriodStart,
                PeriodEnd = b.PeriodEnd,
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                Status = b.Status.ToString(),
                MockTransferRef = b.MockTransferRef,
                ItemsCount = b.Items.Count,
                CreatedAt = b.CreatedAt,
                ApprovedAt = b.ApprovedAt,
                PaidAt = b.PaidAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> GetBatchAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _db.PayoutBatches
            .AsNoTracking()
            .Include(b => b.Items)
                .ThenInclude(i => i.Teacher)
                    .ThenInclude(t => t.User)
            .Include(b => b.Items)
                .ThenInclude(i => i.EarningLines)
                    .ThenInclude(l => l.Enrollment)
                        .ThenInclude(e => e.Course)
            .Include(b => b.Items)
                .ThenInclude(i => i.EarningLines)
                    .ThenInclude(l => l.Enrollment)
                        .ThenInclude(e => e.CourseSchedules)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

        if (batch == null)
            return null;

        var enrollmentIds = batch.Items
            .SelectMany(i => i.EarningLines)
            .Select(l => l.EnrollmentId)
            .Distinct()
            .ToList();

        var refundsByEnrollment = await _db.Refunds
            .AsNoTracking()
            .Where(r => enrollmentIds.Contains(r.EnrollmentId) && r.Status == RefundStatus.Succeeded)
            .GroupBy(r => r.EnrollmentId)
            .Select(g => new { EnrollmentId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.EnrollmentId, x => x.Total, cancellationToken);

        var commissionByEnrollment = await _db.Enrollments
            .AsNoTracking()
            .Where(e => enrollmentIds.Contains(e.Id))
            .Select(e => new
            {
                e.Id,
                PlatformShare = e.PricingSnapshot != null ? e.PricingSnapshot.PlatformShare : 0m
            })
            .ToDictionaryAsync(x => x.Id, x => x.PlatformShare, cancellationToken);

        return new AdminPayoutBatchDto
        {
            Id = batch.Id,
            PeriodStart = batch.PeriodStart,
            PeriodEnd = batch.PeriodEnd,
            TotalAmount = batch.TotalAmount,
            Currency = batch.Currency,
            Status = batch.Status.ToString(),
            MockTransferRef = batch.MockTransferRef,
            ItemsCount = batch.Items.Count,
            CreatedAt = batch.CreatedAt,
            ApprovedAt = batch.ApprovedAt,
            PaidAt = batch.PaidAt,
            Items = batch.Items.Select(i =>
            {
                var lineDtos = i.EarningLines.Select(l =>
                {
                    var schedules = l.Enrollment?.CourseSchedules?
                        .Where(s => s.Status != ScheduleStatus.Cancelled
                                    && s.Status != ScheduleStatus.Rescheduled)
                        .ToList() ?? [];
                    return new AdminPayoutEarningLineDto
                    {
                        LineId = l.Id,
                        EnrollmentId = l.EnrollmentId,
                        CourseTitle = l.Enrollment?.Course?.Title,
                        CourseScheduleId = l.CourseScheduleId,
                        Amount = l.Amount,
                        Source = l.Source.ToString(),
                        Status = l.Status.ToString(),
                        CreatedAt = l.CreatedAt,
                        FreeSessionsInEnrollment = l.Enrollment?.IsFreeTrial == true ? 1 : 0,
                        SessionsCompleted = schedules.Count(s => s.Status == ScheduleStatus.Completed)
                    };
                }).ToList();

                var itemEnrollmentIds = lineDtos.Select(x => x.EnrollmentId).Distinct().ToList();
                var commission = itemEnrollmentIds.Sum(id =>
                    commissionByEnrollment.GetValueOrDefault(id));
                var refunds = itemEnrollmentIds.Sum(id =>
                    refundsByEnrollment.GetValueOrDefault(id));

                return new AdminPayoutItemDto
                {
                    Id = i.Id,
                    TeacherId = i.TeacherId,
                    TeacherName = i.Teacher?.User != null
                        ? ((i.Teacher.User.FirstName ?? "") + " " + (i.Teacher.User.LastName ?? "")).Trim()
                        : null,
                    Amount = i.Amount,
                    Currency = i.Currency,
                    LinesCount = lineDtos.Count,
                    CommissionAmount = commission,
                    RefundsAmount = refunds,
                    TransferrableAmount = i.Amount,
                    Lines = lineDtos
                };
            }).ToList()
        };
    }
}
