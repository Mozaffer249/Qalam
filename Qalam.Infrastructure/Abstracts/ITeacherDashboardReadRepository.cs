using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Results;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherDashboardReadRepository
{
    Task<List<TeacherMySessionListItemDto>> GetMySessionsAsync(
        int teacherId,
        string filter,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TeacherMySessionDetailDto?> GetMySessionByIdAsync(
        int teacherId,
        int scheduledSessionId,
        CancellationToken cancellationToken = default);

    Task<TeacherFinanceSummaryDto> GetFinanceSummaryAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<TeacherFinanceTransactionDto>> GetFinanceTransactionsAsync(
        int teacherId,
        string? typeFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TeacherNotificationsPageDto> GetNotificationsAsync(
        int teacherId,
        bool unreadOnly,
        CancellationToken cancellationToken = default);

    Task<bool> MarkNotificationReadAsync(
        int teacherId,
        int notificationId,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllNotificationsReadAsync(
        int teacherId,
        CancellationToken cancellationToken = default);
}
