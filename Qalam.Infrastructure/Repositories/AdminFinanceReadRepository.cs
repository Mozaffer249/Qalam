using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class AdminFinanceReadRepository : IAdminFinanceReadRepository
{
    private readonly ApplicationDBContext _context;

    public AdminFinanceReadRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<FinanceAggregateProjection> GetAggregatesAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var payments = _context.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.Refunded);
        var refunds = _context.Refunds.AsNoTracking()
            .Where(r => r.Status == RefundStatus.Succeeded);
        var lines = _context.TeacherEarningLines.AsNoTracking();
        var batches = _context.PayoutBatches.AsNoTracking();

        if (fromUtc.HasValue)
        {
            payments = payments.Where(p => p.CreatedAt >= fromUtc.Value);
            refunds = refunds.Where(r => r.CreatedAt >= fromUtc.Value);
            lines = lines.Where(l => l.CreatedAt >= fromUtc.Value);
            batches = batches.Where(b => b.CreatedAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            payments = payments.Where(p => p.CreatedAt <= toUtc.Value);
            refunds = refunds.Where(r => r.CreatedAt <= toUtc.Value);
            lines = lines.Where(l => l.CreatedAt <= toUtc.Value);
            batches = batches.Where(b => b.CreatedAt <= toUtc.Value);
        }

        var totalCollected = await payments.SumAsync(p => p.TotalAmount, cancellationToken);
        var totalRefunds = await refunds.SumAsync(r => r.Amount, cancellationToken);
        var pendingPayments = await _context.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Pending)
            .SumAsync(p => p.TotalAmount, cancellationToken);

        var teacherPending = await lines
            .Where(l => l.Status == TeacherEarningLineStatus.Pending
                        || l.Status == TeacherEarningLineStatus.OnHold)
            .SumAsync(l => l.Amount, cancellationToken);

        var teacherPaid = await lines
            .Where(l => l.Status == TeacherEarningLineStatus.IncludedInPayout
                        && l.PayoutItem != null
                        && l.PayoutItem.PayoutBatch.Status == PayoutBatchStatus.Paid)
            .SumAsync(l => l.Amount, cancellationToken);

        var payoutsDraft = await batches
            .Where(b => b.Status == PayoutBatchStatus.Draft)
            .SumAsync(b => b.TotalAmount, cancellationToken);
        var payoutsApproved = await batches
            .Where(b => b.Status == PayoutBatchStatus.Approved)
            .SumAsync(b => b.TotalAmount, cancellationToken);
        var payoutsPaid = await batches
            .Where(b => b.Status == PayoutBatchStatus.Paid)
            .SumAsync(b => b.TotalAmount, cancellationToken);

        var platformCommission = await (
            from ep in _context.EnrollmentPayments.AsNoTracking()
            join p in payments on ep.PaymentId equals p.Id
            join part in _context.EnrollmentParticipants.AsNoTracking() on ep.EnrollmentParticipantId equals part.Id
            join e in _context.Enrollments.AsNoTracking() on part.EnrollmentId equals e.Id
            where e.PricingSnapshot != null
            select e.PricingSnapshot!.PlatformShare
        ).SumAsync(cancellationToken);

        var freeTrialImpact = await _context.Enrollments.AsNoTracking()
            .Where(e => e.IsFreeTrial && e.AmountDue == 0)
            .Join(_context.TeacherEarningLines.AsNoTracking(),
                e => e.Id,
                l => l.EnrollmentId,
                (e, l) => l.Amount)
            .SumAsync(cancellationToken);

        var bySource = await (
            from p in payments
            join ep in _context.EnrollmentPayments.AsNoTracking() on p.Id equals ep.PaymentId
            join part in _context.EnrollmentParticipants.AsNoTracking() on ep.EnrollmentParticipantId equals part.Id
            join e in _context.Enrollments.AsNoTracking() on part.EnrollmentId equals e.Id
            group p by e.Source into g
            select new AdminRevenueBySourceDto
            {
                Source = g.Key.ToString(),
                Label = g.Key.ToString(),
                Amount = g.Sum(x => x.TotalAmount),
                Count = g.Count()
            }
        ).ToListAsync(cancellationToken);

        return new FinanceAggregateProjection
        {
            TotalCollected = totalCollected,
            TotalRefunds = totalRefunds,
            TeacherEarningsPending = teacherPending,
            TeacherEarningsPaid = teacherPaid,
            PayoutsDraft = payoutsDraft,
            PayoutsApproved = payoutsApproved,
            PayoutsPaid = payoutsPaid,
            PlatformCommission = platformCommission,
            FreeTrialImpact = freeTrialImpact,
            PendingPayments = pendingPayments,
            RevenueBySource = bySource
        };
    }

    public async Task<(List<AdminFinanceTransactionDto> Items, int TotalCount)> ListTransactionsAsync(
        AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default)
    {
        var all = await BuildUnifiedTransactionsAsync(filter, cancellationToken);
        var totalCount = all.Count;
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize switch
        {
            < 1 => 25,
            > 100 => 100,
            _ => filter.PageSize
        };

        var items = all
            .OrderByDescending(t => t.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, totalCount);
    }

    public async Task<AdminTeacherFinanceSummaryDto?> GetTeacherSummaryAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _context.Teachers.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == teacherId, cancellationToken);
        if (teacher == null)
            return null;

        var lines = await _context.TeacherEarningLines.AsNoTracking()
            .Where(l => l.TeacherId == teacherId)
            .Select(l => new
            {
                l.Amount,
                l.Status,
                BatchStatus = l.PayoutItem != null ? (PayoutBatchStatus?)l.PayoutItem.PayoutBatch.Status : null,
                l.EnrollmentId
            })
            .ToListAsync(cancellationToken);

        var enrollmentIds = lines.Select(l => l.EnrollmentId).Distinct().ToList();
        var commission = enrollmentIds.Count == 0
            ? 0m
            : await _context.Enrollments.AsNoTracking()
                .Where(e => enrollmentIds.Contains(e.Id) && e.PricingSnapshot != null)
                .SumAsync(e => e.PricingSnapshot!.PlatformShare, cancellationToken);

        var nonVoided = lines.Where(l => l.Status != TeacherEarningLineStatus.Voided).Sum(l => l.Amount);
        var pending = lines.Where(l => l.Status == TeacherEarningLineStatus.Pending).Sum(l => l.Amount);
        var onHold = lines.Where(l => l.Status == TeacherEarningLineStatus.OnHold).Sum(l => l.Amount);
        var paidOut = lines.Where(l =>
            l.Status == TeacherEarningLineStatus.IncludedInPayout
            && l.BatchStatus == PayoutBatchStatus.Paid).Sum(l => l.Amount);
        var voided = lines.Where(l => l.Status == TeacherEarningLineStatus.Voided).Sum(l => l.Amount);

        var refundsImpact = await _context.Refunds.AsNoTracking()
            .Where(r => r.Status == RefundStatus.Succeeded
                        && r.Enrollment.ApprovedByTeacherId == teacherId)
            .SumAsync(r => r.Amount, cancellationToken);

        return new AdminTeacherFinanceSummaryDto
        {
            TeacherId = teacherId,
            TeacherName = teacher.User != null
                ? ((teacher.User.FirstName ?? "") + " " + (teacher.User.LastName ?? "")).Trim()
                : null,
            TotalEarnings = nonVoided,
            Pending = pending + onHold,
            OnHold = onHold,
            Available = pending,
            PaidOut = paidOut,
            RefundsImpact = refundsImpact,
            Deductions = voided,
            PlatformCommission = commission
        };
    }

    public async Task<(List<AdminFinanceTransactionDto> Items, int TotalCount)> ListTeacherTransactionsAsync(
        int teacherId,
        AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default)
    {
        filter.TeacherId = teacherId;
        return await ListTransactionsAsync(filter, cancellationToken);
    }

    public async Task<(List<AdminRevenueRecordDto> Items, int TotalCount)> ListRevenueRecordsAsync(
        AdminRevenueListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var q = from p in _context.Payments.AsNoTracking()
                where p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.Refunded
                join ep in _context.EnrollmentPayments.AsNoTracking() on p.Id equals ep.PaymentId
                join part in _context.EnrollmentParticipants.AsNoTracking() on ep.EnrollmentParticipantId equals part.Id
                join e in _context.Enrollments.AsNoTracking() on part.EnrollmentId equals e.Id
                select new { p, e, part };

        if (filter.FromUtc.HasValue)
            q = q.Where(x => x.p.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            q = q.Where(x => x.p.CreatedAt <= filter.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(filter.Source))
            q = q.Where(x => x.e.Source.ToString() == filter.Source);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.p.Id.ToString().Contains(search)
                || (x.e.Course != null && x.e.Course.Title.ToLower().Contains(search)));
        }

        var totalCount = await q.CountAsync(cancellationToken);
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize switch
        {
            < 1 => 25,
            > 100 => 100,
            _ => filter.PageSize
        };

        var rows = await q
            .OrderByDescending(x => x.p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Payment = x.p,
                Enrollment = x.e,
                StudentId = x.part.StudentId
            })
            .ToListAsync(cancellationToken);

        var paymentIds = rows.Select(r => r.Payment.Id).ToList();
        var refundTotals = await _context.Refunds.AsNoTracking()
            .Where(r => paymentIds.Contains(r.PaymentId) && r.Status == RefundStatus.Succeeded)
            .GroupBy(r => r.PaymentId)
            .Select(g => new { PaymentId = g.Key, Total = g.Sum(r => r.Amount) })
            .ToDictionaryAsync(x => x.PaymentId, x => x.Total, cancellationToken);

        var items = new List<AdminRevenueRecordDto>();
        foreach (var row in rows)
        {
            var e = row.Enrollment;
            var p = row.Payment;
            var teacherEarnings = e.PricingSnapshot != null ? e.PricingSnapshot.TeacherEarnings : 0m;
            var platformCommission = e.PricingSnapshot != null ? e.PricingSnapshot.PlatformShare : 0m;
            var refunds = refundTotals.GetValueOrDefault(p.Id);
            var freeTrialImpact = e.IsFreeTrial && e.AmountDue == 0 ? teacherEarnings : 0m;

            items.Add(new AdminRevenueRecordDto
            {
                Id = p.Id,
                Key = $"pay-{p.Id}",
                PaymentId = p.Id,
                EnrollmentId = e.Id,
                CourseTitle = e.Course != null ? e.Course.Title : null,
                GrossPayment = p.TotalAmount,
                PlatformCommission = platformCommission,
                TeacherEarnings = teacherEarnings,
                Refunds = refunds,
                NetPlatformRevenue = platformCommission - refunds,
                IsFreeTrial = e.IsFreeTrial,
                FreeTrialImpact = freeTrialImpact,
                Source = e.Source.ToString(),
                Status = p.Status.ToString(),
                Currency = p.Currency,
                OccurredAt = p.CreatedAt,
                TeacherId = e.ApprovedByTeacherId,
                StudentId = row.StudentId
            });
        }

        return (items, totalCount);
    }

    public async Task<AdminRevenueDetailDto?> GetRevenueByPaymentIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await ListRevenueRecordsAsync(new AdminRevenueListFilter
        {
            Page = 1,
            PageSize = 1,
            Search = paymentId.ToString()
        }, cancellationToken);

        var baseRecord = items.FirstOrDefault(i => i.PaymentId == paymentId);
        if (baseRecord == null)
            return null;

        var detail = new AdminRevenueDetailDto
        {
            Id = baseRecord.Id,
            Key = baseRecord.Key,
            PaymentId = baseRecord.PaymentId,
            EnrollmentId = baseRecord.EnrollmentId,
            CourseTitle = baseRecord.CourseTitle,
            GrossPayment = baseRecord.GrossPayment,
            PlatformCommission = baseRecord.PlatformCommission,
            TeacherEarnings = baseRecord.TeacherEarnings,
            Refunds = baseRecord.Refunds,
            NetPlatformRevenue = baseRecord.NetPlatformRevenue,
            IsFreeTrial = baseRecord.IsFreeTrial,
            FreeTrialImpact = baseRecord.FreeTrialImpact,
            Source = baseRecord.Source,
            Status = baseRecord.Status,
            Currency = baseRecord.Currency,
            OccurredAt = baseRecord.OccurredAt,
            TeacherId = baseRecord.TeacherId,
            StudentId = baseRecord.StudentId
        };

        detail.PaymentProviderRef = await _context.Payments.AsNoTracking()
            .Where(p => p.Id == paymentId)
            .Select(p => p.ProviderTransactionId)
            .FirstOrDefaultAsync(cancellationToken);

        detail.Timeline =
        [
            new FinanceTimelineEventDto
            {
                EventType = "PaymentSucceeded",
                Label = "Payment succeeded",
                OccurredAt = detail.OccurredAt
            }
        ];

        if (detail.Refunds > 0)
        {
            detail.Timeline.Add(new FinanceTimelineEventDto
            {
                EventType = "RefundIssued",
                Label = "Refund issued",
                OccurredAt = detail.OccurredAt
            });
        }

        return detail;
    }

    public async Task<int?> ResolveTeacherIdForTransactionKeyAsync(
        string transactionKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionKey))
            return null;

        if (transactionKey.StartsWith("earn-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(transactionKey["earn-".Length..], out var lineId))
        {
            return await _context.TeacherEarningLines.AsNoTracking()
                .Where(l => l.Id == lineId)
                .Select(l => (int?)l.TeacherId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (transactionKey.StartsWith("ref-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(transactionKey["ref-".Length..], out var refundId))
        {
            return await _context.Refunds.AsNoTracking()
                .Where(r => r.Id == refundId)
                .Select(r => (int?)r.Enrollment.ApprovedByTeacherId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (transactionKey.StartsWith("payout-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(transactionKey["payout-".Length..], out var itemId))
        {
            return await _context.PayoutItems.AsNoTracking()
                .Where(i => i.Id == itemId)
                .Select(i => (int?)i.TeacherId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private async Task<List<AdminFinanceTransactionDto>> BuildUnifiedTransactionsAsync(
        AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken)
    {
        var result = new List<AdminFinanceTransactionDto>();

        var earningQ = _context.TeacherEarningLines.AsNoTracking().AsQueryable();
        if (filter.TeacherId.HasValue)
            earningQ = earningQ.Where(l => l.TeacherId == filter.TeacherId.Value);
        if (filter.EnrollmentId.HasValue)
            earningQ = earningQ.Where(l => l.EnrollmentId == filter.EnrollmentId.Value);
        if (filter.FromUtc.HasValue)
            earningQ = earningQ.Where(l => l.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            earningQ = earningQ.Where(l => l.CreatedAt <= filter.ToUtc.Value);

        var earnings = await earningQ.Select(l => new AdminFinanceTransactionDto
        {
            Key = "earn-" + l.Id,
            Type = "Earning",
            Title = "Teacher earning",
            Amount = l.Amount,
            Currency = l.Currency,
            Direction = "credit",
            Status = l.Status.ToString(),
            OccurredAt = l.CreatedAt,
            TeacherId = l.TeacherId,
            TeacherName = l.Teacher.User != null
                ? ((l.Teacher.User.FirstName ?? "") + " " + (l.Teacher.User.LastName ?? "")).Trim()
                : null,
            EnrollmentId = l.EnrollmentId,
            CourseTitle = l.Enrollment.Course != null ? l.Enrollment.Course.Title : null,
            ScheduleId = l.CourseScheduleId,
            Reference = "earn-" + l.Id
        }).ToListAsync(cancellationToken);
        result.AddRange(earnings);

        var refundQ = _context.Refunds.AsNoTracking().AsQueryable();
        if (filter.TeacherId.HasValue)
            refundQ = refundQ.Where(r => r.Enrollment.ApprovedByTeacherId == filter.TeacherId.Value);
        if (filter.EnrollmentId.HasValue)
            refundQ = refundQ.Where(r => r.EnrollmentId == filter.EnrollmentId.Value);
        if (filter.FromUtc.HasValue)
            refundQ = refundQ.Where(r => r.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            refundQ = refundQ.Where(r => r.CreatedAt <= filter.ToUtc.Value);

        var refunds = await refundQ.Select(r => new AdminFinanceTransactionDto
        {
            Key = "ref-" + r.Id,
            Type = "Refund",
            Title = "Refund",
            Description = r.Reason,
            Amount = r.Amount,
            Currency = r.Currency,
            Direction = "debit",
            Status = r.Status.ToString(),
            OccurredAt = r.CreatedAt,
            TeacherId = r.Enrollment.ApprovedByTeacherId,
            EnrollmentId = r.EnrollmentId,
            CourseTitle = r.Enrollment.Course != null ? r.Enrollment.Course.Title : null,
            Reference = r.ProviderRefundId
        }).ToListAsync(cancellationToken);
        result.AddRange(refunds);

        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            var type = filter.Type.Trim();
            result = result.Where(t =>
                t.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            result = result.Where(t =>
                t.Key.ToLower().Contains(search)
                || (t.Reference != null && t.Reference.ToLower().Contains(search))
                || (t.CourseTitle != null && t.CourseTitle.ToLower().Contains(search))
                || (t.TeacherName != null && t.TeacherName.ToLower().Contains(search))).ToList();
        }

        return result;
    }
}
