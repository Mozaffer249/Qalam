using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class RefundService : IRefundService
{
    private readonly ApplicationDBContext _db;

    public RefundService(ApplicationDBContext db)
    {
        _db = db;
    }

    public async Task<Refund> IssueRefundAsync(
        int paymentId,
        int enrollmentId,
        decimal amount,
        string currency,
        string reason,
        int? initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Refund amount must be positive.");

        var payment = await _db.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {paymentId} not found.");

        if (payment.Status is not PaymentStatus.Succeeded and not PaymentStatus.Refunded)
            throw new InvalidOperationException("Only succeeded payments can be refunded.");

        var alreadyRefunded = payment.Refunds
            .Where(r => r.Status == RefundStatus.Succeeded)
            .Sum(r => r.Amount);
        var remaining = payment.TotalAmount - alreadyRefunded;
        if (amount > remaining + 0.001m)
            throw new InvalidOperationException(
                $"Refund amount {amount} exceeds remaining refundable {remaining}.");

        var refund = new Refund
        {
            PaymentId = paymentId,
            EnrollmentId = enrollmentId,
            Amount = Math.Round(amount, 2),
            Currency = string.IsNullOrWhiteSpace(currency) ? payment.Currency : currency,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Refund" : reason.Trim(),
            Status = RefundStatus.Succeeded,
            ProviderRefundId = ("MOCK-REF-" + Guid.NewGuid().ToString("N"))[..24].ToUpperInvariant(),
            InitiatedByUserId = initiatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Refunds.Add(refund);

        var newTotal = alreadyRefunded + refund.Amount;
        var isFullRefund = newTotal >= payment.TotalAmount - 0.001m;
        if (isFullRefund)
            payment.Status = PaymentStatus.Refunded;

        if (payment.Status == PaymentStatus.Refunded)
        {
            var enrollmentPayments = await _db.EnrollmentPayments
                .Where(ep => ep.PaymentId == paymentId)
                .ToListAsync(cancellationToken);
            foreach (var ep in enrollmentPayments)
                ep.Status = PaymentStatus.Refunded;
        }

        await VoidTeacherEarningsForRefundAsync(
            enrollmentId,
            refund.Amount,
            payment.TotalAmount,
            isFullRefund,
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return refund;
    }

    public async Task<IReadOnlyList<Refund>> RefundEnrollmentPaymentsAsync(
        int enrollmentId,
        string reason,
        int? initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var paymentIds = await _db.EnrollmentPayments
            .AsNoTracking()
            .Where(ep => ep.EnrollmentParticipant.EnrollmentId == enrollmentId
                         && (ep.Status == PaymentStatus.Succeeded
                             || ep.Payment.Status == PaymentStatus.Succeeded))
            .Select(ep => ep.PaymentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var results = new List<Refund>();
        foreach (var paymentId in paymentIds)
        {
            var payment = await _db.Payments
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
            if (payment == null)
                continue;

            var alreadyRefunded = payment.Refunds
                .Where(r => r.Status == RefundStatus.Succeeded)
                .Sum(r => r.Amount);
            var remaining = payment.TotalAmount - alreadyRefunded;
            if (remaining <= 0)
                continue;

            var refund = await IssueRefundAsync(
                paymentId,
                enrollmentId,
                remaining,
                payment.Currency,
                reason,
                initiatedByUserId,
                cancellationToken);
            results.Add(refund);
        }

        return results;
    }

    public async Task<List<AdminRefundListItemDto>> ListAsync(
        AdminRefundListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Refunds.AsNoTracking().AsQueryable();

        if (filter.Status.HasValue)
            q = q.Where(r => r.Status == filter.Status.Value);
        if (filter.EnrollmentId.HasValue)
            q = q.Where(r => r.EnrollmentId == filter.EnrollmentId.Value);
        if (filter.FromUtc.HasValue)
            q = q.Where(r => r.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            q = q.Where(r => r.CreatedAt <= filter.ToUtc.Value);

        return await q
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminRefundListItemDto
            {
                Id = r.Id,
                PaymentId = r.PaymentId,
                EnrollmentId = r.EnrollmentId,
                Amount = r.Amount,
                Currency = r.Currency,
                Reason = r.Reason,
                Status = r.Status.ToString(),
                ProviderRefundId = r.ProviderRefundId,
                CreatedAt = r.CreatedAt,
                CourseTitle = r.Enrollment.Course != null ? r.Enrollment.Course.Title : null,
                PayerName = r.Payment.PayerUser != null
                    ? ((r.Payment.PayerUser.FirstName ?? "") + " " + (r.Payment.PayerUser.LastName ?? "")).Trim()
                    : null
            })
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminRefundDetailDto?> GetByIdAsync(
        int refundId,
        CancellationToken cancellationToken = default)
    {
        var r = await _db.Refunds
            .AsNoTracking()
            .Where(x => x.Id == refundId)
            .Select(x => new
            {
                x.Id,
                x.PaymentId,
                x.EnrollmentId,
                x.Amount,
                x.Currency,
                x.Reason,
                x.Status,
                x.ProviderRefundId,
                x.CreatedAt,
                x.InitiatedByUserId,
                PaymentTotal = x.Payment.TotalAmount,
                CourseTitle = x.Enrollment.Course != null ? x.Enrollment.Course.Title : null,
                PayerName = x.Payment.PayerUser != null
                    ? ((x.Payment.PayerUser.FirstName ?? "") + " " + (x.Payment.PayerUser.LastName ?? "")).Trim()
                    : null,
                RefundedTotal = x.Payment.Refunds
                    .Where(rr => rr.Status == RefundStatus.Succeeded)
                    .Sum(rr => rr.Amount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (r == null)
            return null;

        var schedules = await _db.CourseSchedules
            .AsNoTracking()
            .Where(s => s.EnrollmentId == r.EnrollmentId
                        && s.Status != ScheduleStatus.Cancelled
                        && s.Status != ScheduleStatus.Rescheduled)
            .Select(s => s.Status)
            .ToListAsync(cancellationToken);

        var used = schedules.Count(s => s == ScheduleStatus.Completed);
        var unused = Math.Max(0, schedules.Count - used);

        var lines = await _db.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.EnrollmentId == r.EnrollmentId)
            .Select(l => new
            {
                l.Status,
                l.Amount,
                BatchStatus = l.PayoutItem != null
                    ? (PayoutBatchStatus?)l.PayoutItem.PayoutBatch.Status
                    : null
            })
            .ToListAsync(cancellationToken);

        var voided = lines
            .Where(l => l.Status == TeacherEarningLineStatus.Voided)
            .Sum(l => l.Amount);
        var hasPaid = lines.Any(l =>
            l.Status == TeacherEarningLineStatus.IncludedInPayout
            && l.BatchStatus == PayoutBatchStatus.Paid);
        var hasVoidedPending = voided > 0;

        var payoutImpact = "None";
        if (hasPaid)
            payoutImpact = "AlreadyPaid";
        else if (hasVoidedPending)
            payoutImpact = "VoidedPending";

        var platformBear = Math.Max(0m, Math.Round(r.Amount - voided, 2, MidpointRounding.AwayFromZero));

        return new AdminRefundDetailDto
        {
            Id = r.Id,
            PaymentId = r.PaymentId,
            EnrollmentId = r.EnrollmentId,
            Amount = r.Amount,
            Currency = r.Currency,
            Reason = r.Reason,
            Status = r.Status.ToString(),
            ProviderRefundId = r.ProviderRefundId,
            CreatedAt = r.CreatedAt,
            InitiatedByUserId = r.InitiatedByUserId,
            PaymentTotalAmount = r.PaymentTotal,
            PaymentRefundedTotal = r.RefundedTotal,
            CourseTitle = r.CourseTitle,
            PayerName = r.PayerName,
            SessionsUsed = used,
            SessionsUnused = unused,
            TeacherDeductionAmount = voided,
            PlatformBearAmount = platformBear,
            PayoutImpact = payoutImpact
        };
    }

    /// <summary>
    /// Full payment refund voids all Pending/OnHold lines. Partial refund voids newest Pending/OnHold lines
    /// until the refund amount (or remaining pending) is covered. Paid-batch lines are left alone.
    /// </summary>
    private async Task VoidTeacherEarningsForRefundAsync(
        int enrollmentId,
        decimal refundAmount,
        decimal paymentTotal,
        bool isFullRefund,
        CancellationToken cancellationToken)
    {
        var pending = await _db.TeacherEarningLines
            .Where(l => l.EnrollmentId == enrollmentId
                        && (l.Status == TeacherEarningLineStatus.Pending
                            || l.Status == TeacherEarningLineStatus.OnHold))
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        if (isFullRefund || refundAmount >= paymentTotal - 0.001m)
        {
            foreach (var line in pending)
                line.Status = TeacherEarningLineStatus.Voided;
            return;
        }

        var remaining = refundAmount;
        foreach (var line in pending)
        {
            if (remaining <= 0.001m)
                break;
            line.Status = TeacherEarningLineStatus.Voided;
            remaining -= line.Amount;
        }
    }
}
