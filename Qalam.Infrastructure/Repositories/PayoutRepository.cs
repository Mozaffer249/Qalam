using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class PayoutRepository : IPayoutRepository
{
    private readonly ApplicationDBContext _context;

    public PayoutRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<(List<AdminPendingEarningDto> Items, int TotalCount)> ListPendingEarningsAsync(
        AdminPendingEarningsFilter filter,
        CancellationToken cancellationToken = default)
    {
        var q = _context.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.Status == TeacherEarningLineStatus.Pending);

        if (filter.TeacherId.HasValue)
            q = q.Where(l => l.TeacherId == filter.TeacherId.Value);

        var totalCount = await q.CountAsync(cancellationToken);
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize switch
        {
            < 1 => 25,
            > 100 => 100,
            _ => filter.PageSize
        };

        var items = await q
            .OrderBy(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AdminPendingEarningDto
            {
                Id = l.Id,
                TeacherId = l.TeacherId,
                TeacherName = l.Teacher.User != null
                    ? ((l.Teacher.User.FirstName ?? "") + " " + (l.Teacher.User.LastName ?? "")).Trim()
                    : null,
                EnrollmentId = l.EnrollmentId,
                CourseTitle = l.Enrollment.Course != null ? l.Enrollment.Course.Title : null,
                CourseScheduleId = l.CourseScheduleId,
                Amount = l.Amount,
                Currency = l.Currency,
                Source = l.Source.ToString(),
                IsFreeTrialEnrollment = l.Enrollment.IsFreeTrial,
                FreeSessionsInEnrollment = l.Enrollment.IsFreeTrial ? 1 : 0,
                CreatedAt = l.CreatedAt,
                TransactionKey = "earn-" + l.Id
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<AdminPayoutBatchListItemDto> Items, int TotalCount)> ListBatchesAsync(
        AdminPayoutListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var q = _context.PayoutBatches.AsNoTracking().AsQueryable();

        if (filter.Status.HasValue)
            q = q.Where(b => b.Status == filter.Status.Value);
        if (filter.FromUtc.HasValue)
            q = q.Where(b => b.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            q = q.Where(b => b.CreatedAt <= filter.ToUtc.Value);
        if (filter.TeacherId.HasValue)
            q = q.Where(b => b.Items.Any(i => i.TeacherId == filter.TeacherId.Value));

        var totalCount = await q.CountAsync(cancellationToken);
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize switch
        {
            < 1 => 25,
            > 100 => 100,
            _ => filter.PageSize
        };

        var items = await q
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
                UpdatedAt = b.PaidAt ?? b.ApprovedAt ?? b.CreatedAt,
                ApprovedAt = b.ApprovedAt,
                PaidAt = b.PaidAt
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<PayoutBatch?> GetBatchTrackedAsync(
        int batchId,
        CancellationToken cancellationToken = default) =>
        _context.PayoutBatches.FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

    public Task<PayoutBatch?> GetBatchWithDetailsAsync(
        int batchId,
        CancellationToken cancellationToken = default) =>
        _context.PayoutBatches
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

    public Task<List<TeacherEarningLine>> GetPendingLinesInPeriodAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default) =>
        _context.TeacherEarningLines
            .Where(l => l.Status == TeacherEarningLineStatus.Pending
                        && l.CreatedAt >= start
                        && l.CreatedAt <= end)
            .ToListAsync(cancellationToken);

    public async Task AddBatchAsync(PayoutBatch batch, CancellationToken cancellationToken = default)
    {
        await _context.PayoutBatches.AddAsync(batch, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<Dictionary<int, decimal>> GetRefundsByEnrollmentIdsAsync(
        IReadOnlyList<int> enrollmentIds,
        CancellationToken cancellationToken = default)
    {
        if (enrollmentIds.Count == 0)
            return new Dictionary<int, decimal>();

        return await _context.Refunds
            .AsNoTracking()
            .Where(r => enrollmentIds.Contains(r.EnrollmentId) && r.Status == RefundStatus.Succeeded)
            .GroupBy(r => r.EnrollmentId)
            .Select(g => new { EnrollmentId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.EnrollmentId, x => x.Total, cancellationToken);
    }

    public async Task<Dictionary<int, decimal>> GetCommissionByEnrollmentIdsAsync(
        IReadOnlyList<int> enrollmentIds,
        CancellationToken cancellationToken = default)
    {
        if (enrollmentIds.Count == 0)
            return new Dictionary<int, decimal>();

        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => enrollmentIds.Contains(e.Id))
            .Select(e => new
            {
                e.Id,
                PlatformShare = e.PricingSnapshot != null ? e.PricingSnapshot.PlatformShare : 0m
            })
            .ToDictionaryAsync(x => x.Id, x => x.PlatformShare, cancellationToken);
    }
}
