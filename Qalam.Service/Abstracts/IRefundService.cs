using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Payment;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Refund ledger. Current implementation is mock-only (<c>MOCK-REF-…</c>).
/// Real PSP (Moyasar/Stripe/etc.) refunds land later via a provider behind this interface;
/// list/API shape stays first-class <see cref="Qalam.Data.Entity.Payment.Refund"/> rows.
/// </summary>
public interface IRefundService
{
    Task<Refund> IssueRefundAsync(
        int paymentId,
        int enrollmentId,
        decimal amount,
        string currency,
        string reason,
        int? initiatedByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Refund>> RefundEnrollmentPaymentsAsync(
        int enrollmentId,
        string reason,
        int? initiatedByUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminRefundListItemDto>> ListAsync(
        AdminRefundListFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminRefundDetailDto?> GetByIdAsync(int refundId, CancellationToken cancellationToken = default);
}
