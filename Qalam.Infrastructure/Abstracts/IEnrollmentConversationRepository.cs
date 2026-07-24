using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IEnrollmentConversationRepository : IGenericRepositoryAsync<EnrollmentConversation>
{
    Task<EnrollmentConversation?> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find-or-create the conversation for an enrollment. Unique index on EnrollmentId guards concurrency.
    /// </summary>
    Task<EnrollmentConversation> EnsureExistsAsync(
        int enrollmentId,
        int teacherId,
        int studentUserId,
        CancellationToken cancellationToken = default);

    Task<EnrollmentConversationDto?> GetHeaderDtoAsync(
        int conversationId,
        EnrollmentConversationCaller caller,
        CancellationToken cancellationToken = default);

    Task<EnrollmentConversationMessage> AppendMessageAsync(
        int conversationId,
        int? senderUserId,
        EnrollmentMessageType type,
        string content,
        CancellationToken cancellationToken = default);

    Task<EnrollmentConversationMessagesPageDto> GetMessagesPageAsync(
        int conversationId,
        DateTime? cursorSentAt,
        int take,
        bool olderDirection,
        CancellationToken cancellationToken = default);

    Task MarkReadAsync(int conversationId, EnrollmentConversationCaller caller, CancellationToken cancellationToken = default);

    Task<EnrollmentConversationParticipantInfo?> ResolveParticipantAsync(
        int conversationId,
        int userId,
        CancellationToken cancellationToken = default);
}

public enum EnrollmentConversationCaller
{
    Teacher = 1,
    Student = 2
}

public record EnrollmentConversationParticipantInfo(
    int ConversationId,
    int EnrollmentId,
    int TeacherId,
    int TeacherUserId,
    int StudentUserId,
    EnrollmentConversationCaller CallerRole);
