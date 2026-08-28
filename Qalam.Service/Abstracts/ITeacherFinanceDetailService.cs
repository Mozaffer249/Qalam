using Qalam.Data.DTOs.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherFinanceDetailService
{
    Task<TeacherFinanceTransactionDetailDto?> GetTransactionDetailAsync(
        int teacherId,
        string transactionKey,
        CancellationToken cancellationToken = default);
}
