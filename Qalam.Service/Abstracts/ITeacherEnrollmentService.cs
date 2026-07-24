using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface ITeacherEnrollmentService
{
    Task<PaginatedResult<TeacherEnrollmentListItemDto>?> GetEnrollmentsForTeacherAsync(
        int userId,
        EnrollmentStatus? status,
        EnrollmentSource? source,
        EnrollmentKind? kind,
        TeacherEnrollmentSourceBadge? sourceBadge,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<TeacherEnrollmentListItemDto>?> GetCourseEnrollmentsAsync(
        int userId,
        int courseId,
        EnrollmentStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TeacherEnrollmentDetailDto?> GetEnrollmentByIdAsync(
        int userId,
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string Message, bool Forbidden)> RemindPaymentAsync(
        int userId,
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task<(TeacherEnrollmentInvoiceDto? Dto, string? Error, bool Forbidden)> GetInvoiceAsync(
        int userId,
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task<(EnrollmentConversationDto? Dto, string? Error, bool Forbidden)> GetOrCreateConversationAsync(
        int userId,
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task<(EnrollmentConversationMessagesPageDto? Page, bool Forbidden)> GetConversationMessagesAsync(
        int userId,
        int conversationId,
        string? cursor,
        string? direction,
        int take,
        CancellationToken cancellationToken = default);

    Task<(EnrollmentConversationMessageDto? Dto, string? Error, bool Forbidden)> PostConversationMessageAsync(
        int userId,
        int conversationId,
        string? content,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, bool Forbidden)> MarkConversationReadAsync(
        int userId,
        int conversationId,
        CancellationToken cancellationToken = default);
}
