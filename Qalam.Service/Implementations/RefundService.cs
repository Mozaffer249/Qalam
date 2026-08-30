using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class RefundService : IRefundService
{
    private readonly IRefundRepository _refunds;
    private readonly ITeacherFinanceImpactService _financeImpact;

    public RefundService(
        IRefundRepository refunds,
        ITeacherFinanceImpactService financeImpact)
    {
        _refunds = refunds;
        _financeImpact = financeImpact;
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

        var payment = await _refunds.GetTrackedPaymentWithRefundsAsync(paymentId, cancellationToken)
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

        await _refunds.AddRefundAsync(refund, cancellationToken);

        var newTotal = alreadyRefunded + refund.Amount;
        var isFullRefund = newTotal >= payment.TotalAmount - 0.001m;
        if (isFullRefund)
            payment.Status = PaymentStatus.Refunded;

        if (payment.Status == PaymentStatus.Refunded)
        {
            var enrollmentPayments = await _refunds.GetEnrollmentPaymentsForPaymentAsync(
                paymentId, cancellationToken);
            foreach (var ep in enrollmentPayments)
                ep.Status = PaymentStatus.Refunded;
        }

        var voidedAmount = await VoidTeacherEarningsForRefundAsync(
            enrollmentId,
            refund.Amount,
            payment.TotalAmount,
            isFullRefund,
            cancellationToken);

        if (voidedAmount > 0
            && await _financeImpact.IsAlreadyPaidForEnrollmentAsync(enrollmentId, cancellationToken))
        {
            var teacherId = await _refunds.GetTeacherIdForEnrollmentAsync(enrollmentId, cancellationToken);
            if (teacherId > 0)
            {
                await _financeImpact.RecordSettlementForAlreadyPaidAsync(
                    teacherId,
                    voidedAmount,
                    refund.Currency,
                    refund.Id,
                    complaintId: null,
                    earningLineId: null,
                    initiatedByUserId,
                    cancellationToken);
            }
        }

        await _refunds.SaveChangesAsync(cancellationToken);
        return refund;
    }

    public async Task<IReadOnlyList<Refund>> RefundEnrollmentPaymentsAsync(
        int enrollmentId,
        string reason,
        int? initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var paymentIds = await _refunds.GetRefundablePaymentIdsForEnrollmentAsync(
            enrollmentId, cancellationToken);

        var results = new List<Refund>();
        foreach (var paymentId in paymentIds)
        {
            var payment = await _refunds.GetTrackedPaymentWithRefundsAsync(paymentId, cancellationToken);
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

    public async Task<PagedResult<AdminRefundListItemDto>> ListAsync(
        AdminRefundListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _refunds.ListAsync(filter, cancellationToken);
        return new PagedResult<AdminRefundListItemDto>
        {
            Items = items,
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = filter.PageSize < 1 ? 25 : filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AdminRefundDetailDto?> GetByIdAsync(
        int refundId,
        CancellationToken cancellationToken = default)
    {
        var r = await _refunds.GetDetailProjectionAsync(refundId, cancellationToken);
        if (r == null)
            return null;

        var schedules = await _refunds.GetScheduleStatusesForEnrollmentAsync(
            r.EnrollmentId, cancellationToken);
        var used = schedules.Count(s => s.Status == ScheduleStatus.Completed.ToString());
        var unused = Math.Max(0, schedules.Count - used);

        var lines = await _refunds.GetEarningLinesForEnrollmentAsync(r.EnrollmentId, cancellationToken);
        var voided = lines
            .Where(l => l.Status == TeacherEarningLineStatus.Voided.ToString())
            .Sum(l => l.Amount);
        var hasPaid = lines.Any(l =>
            l.Status == TeacherEarningLineStatus.IncludedInPayout.ToString()
            && l.BatchStatus == PayoutBatchStatus.Paid.ToString());
        var hasVoidedPending = voided > 0;

        var payoutImpact = "None";
        if (hasPaid)
            payoutImpact = "AlreadyPaid";
        else if (hasVoidedPending)
            payoutImpact = "VoidedPending";

        var platformBear = Math.Max(0m, Math.Round(r.Amount - voided, 2, MidpointRounding.AwayFromZero));
        var complaintId = await _refunds.GetComplaintIdForRefundAsync(refundId, cancellationToken);
        var linkedLineIds = lines
            .Where(l => l.Status == TeacherEarningLineStatus.Voided.ToString())
            .Select(l => l.Id)
            .ToList();

        var timeline = new List<FinanceTimelineEventDto>
        {
            new()
            {
                EventType = "Created",
                Label = "Refund created",
                OccurredAt = r.CreatedAt,
                ActorName = r.InitiatedByName
            }
        };

        if (r.Status == RefundStatus.Succeeded.ToString())
        {
            timeline.Add(new FinanceTimelineEventDto
            {
                EventType = "Processed",
                Label = "Refund processed",
                OccurredAt = r.CreatedAt,
                Notes = r.ProviderRefundId
            });
        }

        foreach (var lineId in linkedLineIds)
        {
            timeline.Add(new FinanceTimelineEventDto
            {
                EventType = "EarningVoided",
                Label = $"Teacher earning line #{lineId} voided",
                OccurredAt = r.CreatedAt
            });
        }

        return new AdminRefundDetailDto
        {
            Id = r.Id,
            PaymentId = r.PaymentId,
            EnrollmentId = r.EnrollmentId,
            Amount = r.Amount,
            Currency = r.Currency,
            Reason = r.Reason,
            Status = r.Status,
            ProviderRefundId = r.ProviderRefundId,
            CreatedAt = r.CreatedAt,
            ProcessedAt = r.Status == RefundStatus.Succeeded.ToString() ? r.CreatedAt : null,
            InitiatedByUserId = r.InitiatedByUserId,
            InitiatedByName = r.InitiatedByName,
            PaymentTotalAmount = r.PaymentTotal,
            PaymentRefundedTotal = r.RefundedTotal,
            CourseTitle = r.CourseTitle,
            PayerName = r.PayerName,
            TeacherId = r.TeacherId,
            TeacherName = r.TeacherName,
            StudentId = r.StudentId,
            StudentName = r.StudentName,
            ScheduleId = r.ScheduleId,
            SessionLabel = r.SessionLabel,
            OriginalPaymentAmount = r.PaymentTotal,
            TransactionKey = $"ref-{r.Id}",
            Description = $"Refund to student — {r.Reason}",
            SessionsUsed = used,
            SessionsUnused = unused,
            TeacherDeductionAmount = voided,
            PlatformBearAmount = platformBear,
            PayoutImpact = payoutImpact,
            SessionComplaintId = complaintId,
            LinkedEarningLineIds = linkedLineIds,
            Timeline = timeline,
            PaymentProviderRef = r.PaymentProviderRef
        };
    }

    private async Task<decimal> VoidTeacherEarningsForRefundAsync(
        int enrollmentId,
        decimal refundAmount,
        decimal paymentTotal,
        bool isFullRefund,
        CancellationToken cancellationToken)
    {
        var pending = await _refunds.GetPendingEarningLinesForEnrollmentAsync(
            enrollmentId, cancellationToken);

        if (pending.Count == 0)
            return 0m;

        decimal voidedTotal = 0m;

        if (isFullRefund || refundAmount >= paymentTotal - 0.001m)
        {
            foreach (var line in pending)
            {
                line.Status = TeacherEarningLineStatus.Voided;
                voidedTotal += line.Amount;
            }
            return voidedTotal;
        }

        var remaining = refundAmount;
        foreach (var line in pending)
        {
            if (remaining <= 0.001m)
                break;
            line.Status = TeacherEarningLineStatus.Voided;
            voidedTotal += line.Amount;
            remaining -= line.Amount;
        }

        return voidedTotal;
    }
}
