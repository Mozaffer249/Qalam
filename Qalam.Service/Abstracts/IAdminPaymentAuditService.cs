using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Payment;

namespace Qalam.Service.Abstracts;

public interface IAdminPaymentAuditService
{
    Task<PagedResult<AdminPaymentTransactionEventDto>> ListEventsAsync(
        AdminPaymentTransactionEventFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminPaymentTransactionEventDto>> ListEventsForPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminPaymentReconciliationRunDto>> ListReconciliationRunsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminPaymentReconciliationRunDto?> GetReconciliationRunAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PaymentReconciliationRun> StartReconciliationAsync(
        StartPaymentReconciliationRequestDto request,
        int? triggeredByUserId,
        CancellationToken cancellationToken = default);
}
