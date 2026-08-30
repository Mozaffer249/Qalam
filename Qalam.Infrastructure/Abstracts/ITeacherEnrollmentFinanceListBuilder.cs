using Qalam.Data.DTOs.Teacher;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherEnrollmentFinanceListBuilder
{
    Task<List<TeacherFinanceTransactionDto>> BuildAsync(
        int teacherId,
        int? enrollmentId,
        string? typeFilter,
        CancellationToken cancellationToken = default);
}
