using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Refund ledger. Current implementation is mock-only (<c>MOCK-REF-…</c>).
/// Real PSP (Moyasar/Stripe/etc.) refunds land later via a provider behind this interface;
/// list/API shape stays first-class <see cref="Qalam.Data.Entity.Payment.Refund"/> rows.
/// </summary>
public interface IRefundService
{
    /// <param name="destination">
    /// Wallet (default) credits the payer's wallet; OriginalMethod refunds through the payment's gateway.
    /// Wallet-funded payments always refund to the wallet.
    /// </param>
    Task<Refund> IssueRefundAsync(
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
        bool requiresApproval = false);

    /// <summary>Settles a refund that was created in <see cref="RefundStatus.RequiresApproval"/>.</summary>
    Task<Refund> ApproveRefundAsync(
        int refundId,
        int approvedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Rejects a refund awaiting approval; no money is moved.</summary>
    Task<Refund> RejectRefundAsync(
        int refundId,
        int rejectedByUserId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Refund>> RefundEnrollmentPaymentsAsync(
        int enrollmentId,
        string reason,
        int? initiatedByUserId,
        CancellationToken cancellationToken = default,
        RefundDestination destination = RefundDestination.Wallet);

    Task<PagedResult<AdminRefundListItemDto>> ListAsync(
        AdminRefundListFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminRefundDetailDto?> GetByIdAsync(int refundId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Set when a refund is issued by a policy case. The case owns teacher-earning effects,
/// so the refund does not void earnings on its own.
/// </summary>
public record RefundPolicyOptions(int PolicyCaseId, decimal FeeAmount);
