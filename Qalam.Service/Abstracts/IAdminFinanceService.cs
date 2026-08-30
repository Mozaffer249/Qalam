using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Service.Abstracts;

public interface IAdminFinanceService
{
    Task<AdminFinanceSummaryDto> GetSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminFinanceTransactionDto>> ListTransactionsAsync(
        AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminTeacherFinanceSummaryDto?> GetTeacherSummaryAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminFinanceTransactionDto>> ListTeacherTransactionsAsync(
        int teacherId,
        AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminRevenueSummaryDto> GetRevenueSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminRevenueRecordDto>> ListRevenueRecordsAsync(
        AdminRevenueListFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminRevenueDetailDto?> GetRevenueByIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default);
}

public interface IAdminFinanceTransactionService
{
    Task<TeacherFinanceTransactionDetailDto?> GetTransactionDetailAsync(
        string transactionKey,
        CancellationToken cancellationToken = default);
}
