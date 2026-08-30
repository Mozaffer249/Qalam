using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AdminFinanceService : IAdminFinanceService
{
    private readonly IAdminFinanceReadRepository _finance;

    public AdminFinanceService(IAdminFinanceReadRepository finance)
    {
        _finance = finance;
    }

    public async Task<AdminFinanceSummaryDto> GetSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var agg = await _finance.GetAggregatesAsync(fromUtc, toUtc, cancellationToken);
        return new AdminFinanceSummaryDto
        {
            TotalCollected = agg.TotalCollected,
            TotalRefunds = agg.TotalRefunds,
            TeacherEarningsPending = agg.TeacherEarningsPending,
            TeacherEarningsPaid = agg.TeacherEarningsPaid,
            PlatformNet = agg.PlatformCommission - agg.TotalRefunds,
            PayoutsDraft = agg.PayoutsDraft,
            PayoutsApproved = agg.PayoutsApproved,
            PayoutsPaid = agg.PayoutsPaid,
            Currency = "SAR",
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
    }

    public async Task<PagedResult<AdminFinanceTransactionDto>> ListTransactionsAsync(
        AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _finance.ListTransactionsAsync(filter, cancellationToken);
        return new PagedResult<AdminFinanceTransactionDto>
        {
            Items = items,
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = filter.PageSize < 1 ? 25 : filter.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<AdminTeacherFinanceSummaryDto?> GetTeacherSummaryAsync(
        int teacherId,
        CancellationToken cancellationToken = default) =>
        _finance.GetTeacherSummaryAsync(teacherId, cancellationToken);

    public async Task<PagedResult<AdminFinanceTransactionDto>> ListTeacherTransactionsAsync(
        int teacherId,
        AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _finance.ListTeacherTransactionsAsync(
            teacherId, filter, cancellationToken);
        return new PagedResult<AdminFinanceTransactionDto>
        {
            Items = items,
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = filter.PageSize < 1 ? 25 : filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AdminRevenueSummaryDto> GetRevenueSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var agg = await _finance.GetAggregatesAsync(fromUtc, toUtc, cancellationToken);
        return new AdminRevenueSummaryDto
        {
            TotalRevenue = agg.TotalCollected,
            NetRevenue = agg.PlatformCommission - agg.TotalRefunds,
            PlatformCommission = agg.PlatformCommission,
            TeacherEarnings = agg.TeacherEarningsPending + agg.TeacherEarningsPaid,
            Refunds = agg.TotalRefunds,
            Discounts = 0,
            PendingRevenue = agg.PendingPayments,
            FreeTrialImpact = agg.FreeTrialImpact,
            Currency = "SAR",
            FromUtc = fromUtc,
            ToUtc = toUtc,
            BySource = agg.RevenueBySource
        };
    }

    public async Task<PagedResult<AdminRevenueRecordDto>> ListRevenueRecordsAsync(
        AdminRevenueListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _finance.ListRevenueRecordsAsync(filter, cancellationToken);
        return new PagedResult<AdminRevenueRecordDto>
        {
            Items = items,
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = filter.PageSize < 1 ? 25 : filter.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<AdminRevenueDetailDto?> GetRevenueByIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default) =>
        _finance.GetRevenueByPaymentIdAsync(paymentId, cancellationToken);
}
