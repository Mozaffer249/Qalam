using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AdminFinanceTransactionService : IAdminFinanceTransactionService
{
    private readonly IAdminFinanceReadRepository _finance;
    private readonly ITeacherFinanceDetailService _teacherFinance;

    public AdminFinanceTransactionService(
        IAdminFinanceReadRepository finance,
        ITeacherFinanceDetailService teacherFinance)
    {
        _finance = finance;
        _teacherFinance = teacherFinance;
    }

    public async Task<TeacherFinanceTransactionDetailDto?> GetTransactionDetailAsync(
        string transactionKey,
        CancellationToken cancellationToken = default)
    {
        var teacherId = await _finance.ResolveTeacherIdForTransactionKeyAsync(
            transactionKey, cancellationToken);
        if (!teacherId.HasValue)
            return null;

        return await _teacherFinance.GetTransactionDetailAsync(
            teacherId.Value,
            transactionKey,
            cancellationToken);
    }
}
