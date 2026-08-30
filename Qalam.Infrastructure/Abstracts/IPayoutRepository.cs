using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Abstracts;

public interface IPayoutRepository
{
    Task<(List<AdminPendingEarningDto> Items, int TotalCount)> ListPendingEarningsAsync(
        AdminPendingEarningsFilter filter,
        CancellationToken cancellationToken = default);

    Task<(List<AdminPayoutBatchListItemDto> Items, int TotalCount)> ListBatchesAsync(
        AdminPayoutListFilter filter,
        CancellationToken cancellationToken = default);

    Task<PayoutBatch?> GetBatchTrackedAsync(
        int batchId,
        CancellationToken cancellationToken = default);

    Task<PayoutBatch?> GetBatchWithDetailsAsync(
        int batchId,
        CancellationToken cancellationToken = default);

    Task<List<TeacherEarningLine>> GetPendingLinesInPeriodAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);

    Task AddBatchAsync(PayoutBatch batch, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<int, decimal>> GetRefundsByEnrollmentIdsAsync(
        IReadOnlyList<int> enrollmentIds,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, decimal>> GetCommissionByEnrollmentIdsAsync(
        IReadOnlyList<int> enrollmentIds,
        CancellationToken cancellationToken = default);
}
