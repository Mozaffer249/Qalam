using Qalam.Data.DTOs.Admin;

namespace Qalam.Infrastructure.Abstracts;

public interface IAdminFinanceReadRepository
{
    Task<FinanceAggregateProjection> GetAggregatesAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<(List<AdminFinanceTransactionDto> Items, int TotalCount)> ListTransactionsAsync(
        AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminTeacherFinanceSummaryDto?> GetTeacherSummaryAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<(List<AdminFinanceTransactionDto> Items, int TotalCount)> ListTeacherTransactionsAsync(
        int teacherId,
        AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default);

    Task<(List<AdminRevenueRecordDto> Items, int TotalCount)> ListRevenueRecordsAsync(
        AdminRevenueListFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminRevenueDetailDto?> GetRevenueByPaymentIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<int?> ResolveTeacherIdForTransactionKeyAsync(
        string transactionKey,
        CancellationToken cancellationToken = default);
}

public class FinanceAggregateProjection
{
    public decimal TotalCollected { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TeacherEarningsPending { get; set; }
    public decimal TeacherEarningsPaid { get; set; }
    public decimal PayoutsDraft { get; set; }
    public decimal PayoutsApproved { get; set; }
    public decimal PayoutsPaid { get; set; }
    public decimal PlatformCommission { get; set; }
    public decimal FreeTrialImpact { get; set; }
    public decimal PendingPayments { get; set; }
    public List<AdminRevenueBySourceDto> RevenueBySource { get; set; } = new();
}
