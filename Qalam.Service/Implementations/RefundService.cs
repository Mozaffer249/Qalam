using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Payments;

namespace Qalam.Service.Implementations;

public class RefundService : IRefundService
{
    private readonly IRefundRepository _refunds;
    private readonly ITeacherFinanceImpactService _financeImpact;
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly IPaymentTransactionEventService _events;
    private readonly IStudentWalletService _wallet;
    private readonly IAuditService? _audit;

    public RefundService(
        IRefundRepository refunds,
        ITeacherFinanceImpactService financeImpact,
        IPaymentGatewayResolver gatewayResolver,
        IPaymentTransactionEventService events,
        IStudentWalletService wallet,
        IAuditService? audit = null)
    {
        _refunds = refunds;
        _financeImpact = financeImpact;
        _gatewayResolver = gatewayResolver;
        _events = events;
        _wallet = wallet;
        _audit = audit;
    }

    public async Task<Refund> IssueRefundAsync(
        int paymentId,
        int enrollmentId,
        decimal amount,
        string currency,
        string reason,
        int? initiatedByUserId,
        CancellationToken cancellationToken = default,
        RefundDestination destination = RefundDestination.Wallet,
        int? complaintId = null,
        int? courseScheduleId = null,
        RefundPolicyOptions? policy = null,
        bool requiresApproval = false)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Refund amount must be positive.");

        var payment = await _refunds.GetTrackedPaymentWithRefundsAsync(paymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {paymentId} not found.");
        if (!string.IsNullOrWhiteSpace(payment.Currency))
            currency = payment.Currency;

        if (payment.Status is not PaymentStatus.Succeeded and not PaymentStatus.Refunded)
            throw new InvalidOperationException("Only succeeded payments can be refunded.");

        var alreadyRefunded = payment.Refunds
            .Where(r => r.Status == RefundStatus.Succeeded)
            .Sum(r => r.Amount);
        var remaining = payment.TotalAmount - alreadyRefunded;
        if (amount > remaining + 0.001m)
            throw new InvalidOperationException(
                $"Refund amount {amount} exceeds remaining refundable {remaining}.");

        var toWallet = destination == RefundDestination.Wallet
            || payment.PaymentProvider.Equals(WalletSettings.ProviderName, StringComparison.OrdinalIgnoreCase);

        // Always create the refund row first; settlement happens now (immediate) or on approval.
        var refund = new Refund
        {
            PaymentId = paymentId,
            EnrollmentId = enrollmentId,
            Amount = Math.Round(amount, 2),
            Currency = string.IsNullOrWhiteSpace(currency) ? payment.Currency : currency,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Refund" : reason.Trim(),
            Status = requiresApproval ? RefundStatus.RequiresApproval : RefundStatus.Pending,
            InitiatedByUserId = initiatedByUserId,
            Destination = toWallet ? RefundDestination.Wallet : RefundDestination.OriginalMethod,
            FeeAmount = Math.Round(policy?.FeeAmount ?? 0m, 2),
            PolicyCaseId = policy?.PolicyCaseId,
            CreatedAt = DateTime.UtcNow
        };
        await _refunds.AddRefundAsync(refund, cancellationToken);
        await _refunds.SaveChangesAsync(cancellationToken);

        await _events.RecordAsync(new PaymentTransactionEventRequest
        {
            PaymentId = payment.Id,
            EnrollmentId = enrollmentId,
            PaymentProvider = payment.PaymentProvider,
            Source = PaymentTransactionEventSource.Admin,
            EventType = PaymentTransactionEventType.RefundRequested,
            Result = PaymentTransactionEventResult.Pending,
            StatusBefore = payment.Status,
            Amount = refund.Amount,
            Currency = refund.Currency,
            ProviderPaymentId = payment.ProviderTransactionId,
            ProviderInvoiceId = payment.ProviderInvoiceId,
            Notes = reason
        }, cancellationToken);

        // Gated refunds move no money until an admin approves them.
        if (requiresApproval)
            return refund;

        await SettleRefundAsync(
            refund, payment, enrollmentId, alreadyRefunded,
            complaintId, courseScheduleId, policy, cancellationToken);
        return refund;
    }

    public async Task<Refund> ApproveRefundAsync(
        int refundId,
        int approvedByUserId,
        CancellationToken cancellationToken = default)
    {
        var refund = await _refunds.GetTrackedRefundAsync(refundId, cancellationToken)
            ?? throw new InvalidOperationException($"Refund {refundId} not found.");
        if (refund.Status != RefundStatus.RequiresApproval)
            throw new InvalidOperationException("Only refunds awaiting approval can be approved.");

        var payment = await _refunds.GetTrackedPaymentWithRefundsAsync(refund.PaymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {refund.PaymentId} not found.");

        var alreadyRefunded = payment.Refunds
            .Where(r => r.Status == RefundStatus.Succeeded && r.Id != refund.Id)
            .Sum(r => r.Amount);

        // Reset to Pending so SettleRefundAsync can drive it to its terminal state.
        refund.Status = RefundStatus.Pending;
        if (approvedByUserId > 0)
            refund.InitiatedByUserId ??= approvedByUserId;

        await SettleRefundAsync(
            refund, payment, refund.EnrollmentId, alreadyRefunded,
            complaintId: null, courseScheduleId: null, policy: null, cancellationToken);

        await AuditAsync(
            "Refund.Approved", approvedByUserId, refund,
            $"{{\"statusBefore\":\"RequiresApproval\",\"statusAfter\":\"{refund.Status}\",\"amount\":{refund.Amount},\"currency\":\"{refund.Currency}\",\"destination\":\"{refund.Destination}\"}}");
        return refund;
    }

    public async Task<Refund> RejectRefundAsync(
        int refundId,
        int rejectedByUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var refund = await _refunds.GetTrackedRefundAsync(refundId, cancellationToken)
            ?? throw new InvalidOperationException($"Refund {refundId} not found.");
        if (refund.Status != RefundStatus.RequiresApproval)
            throw new InvalidOperationException("Only refunds awaiting approval can be rejected.");

        refund.Status = RefundStatus.Rejected;
        if (!string.IsNullOrWhiteSpace(reason))
            refund.Reason = $"{refund.Reason} | Rejected: {reason.Trim()}";
        await _refunds.SaveChangesAsync(cancellationToken);

        await _events.RecordAsync(new PaymentTransactionEventRequest
        {
            PaymentId = refund.PaymentId,
            EnrollmentId = refund.EnrollmentId,
            PaymentProvider = string.Empty,
            Source = PaymentTransactionEventSource.Admin,
            EventType = PaymentTransactionEventType.RefundFailed,
            Result = PaymentTransactionEventResult.Failed,
            Amount = refund.Amount,
            Currency = refund.Currency,
            Notes = reason,
            ErrorMessage = "rejected_by_admin"
        }, cancellationToken);

        await AuditAsync(
            "Refund.Rejected", rejectedByUserId, refund,
            $"{{\"statusBefore\":\"RequiresApproval\",\"statusAfter\":\"Rejected\",\"amount\":{refund.Amount},\"currency\":\"{refund.Currency}\",\"reason\":{System.Text.Json.JsonSerializer.Serialize(reason)}}}");

        return refund;
    }

    private async Task AuditAsync(string action, int actorUserId, Refund refund, string details)
    {
        if (_audit == null)
            return;
        await _audit.LogAsync(
            action,
            actorUserId > 0 ? actorUserId : null,
            ipAddress: string.Empty,
            success: true,
            details: details,
            entityType: nameof(Refund),
            entityId: refund.Id.ToString());
    }

    /// <summary>
    /// Moves the money for an already-persisted refund row (wallet credit or gateway),
    /// then applies payment-status, enrollment-payment and teacher-earning effects on success.
    /// Shared by the immediate issue path and the approval path.
    /// </summary>
    private async Task SettleRefundAsync(
        Refund refund,
        Payment payment,
        int enrollmentId,
        decimal alreadyRefunded,
        int? complaintId,
        int? courseScheduleId,
        RefundPolicyOptions? policy,
        CancellationToken cancellationToken)
    {
        var statusBefore = payment.Status;
        var toWallet = refund.Destination == RefundDestination.Wallet
            || payment.PaymentProvider.Equals(WalletSettings.ProviderName, StringComparison.OrdinalIgnoreCase);

        if (toWallet)
        {
            var credit = await _wallet.CreditAsync(new WalletEntryRequest
            {
                UserId = payment.PayerUserId,
                Amount = refund.Amount,
                Type = WalletTransactionType.Refund,
                PaymentId = payment.Id,
                RefundId = refund.Id,
                EnrollmentId = enrollmentId,
                CourseScheduleId = courseScheduleId,
                ComplaintId = complaintId,
                PolicyCaseId = policy?.PolicyCaseId,
                Description = refund.Reason,
                ReasonCode = policy != null ? "POLICY_REFUND" : "REFUND",
                CreatedByUserId = refund.InitiatedByUserId
            }, cancellationToken);

            if (credit.Succeeded)
            {
                refund.Status = RefundStatus.Succeeded;
                refund.ProviderRefundId = $"WALLET-{credit.Transaction!.Id}";
            }
            else
            {
                refund.Status = RefundStatus.Failed;
            }
        }
        else
        {
            var remaining = payment.TotalAmount - alreadyRefunded;
            var (status, providerRefundId) = await RefundThroughGatewayAsync(
                payment, refund.Amount, remaining, cancellationToken);
            refund.Status = status;
            refund.ProviderRefundId = providerRefundId;
        }

        if (refund.Status == RefundStatus.Succeeded)
        {
            var newTotal = alreadyRefunded + refund.Amount;
            var isFullRefund = newTotal >= payment.TotalAmount - 0.001m;
            if (isFullRefund)
                payment.Status = PaymentStatus.Refunded;

            if (payment.Status == PaymentStatus.Refunded)
            {
                var enrollmentPayments = await _refunds.GetEnrollmentPaymentsForPaymentAsync(
                    payment.Id, cancellationToken);
                foreach (var ep in enrollmentPayments)
                    ep.Status = PaymentStatus.Refunded;
            }

            var voidedAmount = policy == null
                ? await VoidTeacherEarningsForRefundAsync(
                    enrollmentId,
                    refund.Amount,
                    payment.TotalAmount,
                    isFullRefund,
                    cancellationToken)
                : 0m;

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
                        refund.InitiatedByUserId,
                        cancellationToken);
                }
            }
        }

        await _refunds.SaveChangesAsync(cancellationToken);

        await _events.RecordAsync(new PaymentTransactionEventRequest
        {
            PaymentId = payment.Id,
            EnrollmentId = enrollmentId,
            PaymentProvider = payment.PaymentProvider,
            Source = PaymentTransactionEventSource.Admin,
            EventType = refund.Status == RefundStatus.Succeeded
                ? PaymentTransactionEventType.RefundSucceeded
                : PaymentTransactionEventType.RefundFailed,
            Result = refund.Status == RefundStatus.Succeeded
                ? PaymentTransactionEventResult.Success
                : PaymentTransactionEventResult.Failed,
            StatusBefore = statusBefore,
            StatusAfter = payment.Status,
            Amount = refund.Amount,
            Currency = refund.Currency,
            ProviderPaymentId = payment.ProviderTransactionId,
            ProviderInvoiceId = payment.ProviderInvoiceId,
            Notes = refund.ProviderRefundId,
            ErrorMessage = refund.Status == RefundStatus.Succeeded ? null : "gateway_refund_pending_or_failed"
        }, cancellationToken);
    }

    private async Task<(RefundStatus Status, string? ProviderRefundId)> RefundThroughGatewayAsync(
        Payment payment,
        decimal amount,
        decimal remaining,
        CancellationToken cancellationToken)
    {
        string? providerRefundId = null;
        var refundStatus = RefundStatus.Succeeded;

        IPaymentGateway gateway;
        try
        {
            gateway = _gatewayResolver.Resolve(payment.PaymentProvider);
        }
        catch (InvalidOperationException)
        {
            gateway = _gatewayResolver.Resolve(MockPaymentGateway.Name);
        }

        var isMock = gateway.ProviderName.Equals(MockPaymentGateway.Name, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(payment.ProviderTransactionId);

        if (!isMock)
        {
            try
            {
                var amountHalalas = MinorUnitConverter.ToHalalas(amount);
                var fullRefund = amount >= remaining - 0.001m;
                var gatewayRefund = await gateway.RefundAsync(
                    payment.ProviderTransactionId!,
                    fullRefund ? null : amountHalalas,
                    cancellationToken);
                providerRefundId = gatewayRefund.Id;
            }
            catch
            {
                refundStatus = RefundStatus.Pending;
                providerRefundId = null;
            }
        }
        else
        {
            var mockRefund = await gateway.RefundAsync(
                payment.ProviderTransactionId ?? "MOCK",
                MinorUnitConverter.ToHalalas(amount),
                cancellationToken);
            providerRefundId = mockRefund.Id;
        }

        return (refundStatus, providerRefundId);
    }

    public async Task<IReadOnlyList<Refund>> RefundEnrollmentPaymentsAsync(
        int enrollmentId,
        string reason,
        int? initiatedByUserId,
        CancellationToken cancellationToken = default,
        RefundDestination destination = RefundDestination.Wallet)
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
                cancellationToken,
                destination);
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
            Destination = r.Destination,
            FeeAmount = r.FeeAmount,
            PolicyCaseId = r.PolicyCaseId,
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
