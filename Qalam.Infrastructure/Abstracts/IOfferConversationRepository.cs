using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IOfferConversationRepository : IGenericRepositoryAsync<OfferConversation>
{
    /// <summary>Look up the conversation row for a (request, teacher) pair, or null.</summary>
    Task<OfferConversation?> GetByRequestAndTeacherAsync(int requestId, int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find-or-create the conversation row for the (request, teacher) pair. Single transactional path —
    /// safe under concurrency because the unique index on (SessionRequestId, TeacherId) blocks a second insert.
    /// </summary>
    Task<OfferConversation> EnsureExistsAsync(int requestId, int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the conversation's SessionOfferId pointer (or clears it). Used when an offer is submitted
    /// or withdrawn — the chat history persists across offer lifecycle.
    /// </summary>
    Task SetCurrentOfferAsync(int conversationId, int? offerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Header projection (the GET /Conversations/by-request payload) keyed off conversationId.
    /// Unread count is computed from the caller's per-role LastReadAt.
    /// </summary>
    Task<OfferConversationDto?> GetHeaderDtoAsync(int conversationId, ConversationCaller caller, CancellationToken cancellationToken = default);

    /// <summary>Append a message and bump LastMessageAt.</summary>
    Task<OfferMessage> AppendMessageAsync(int conversationId, int? senderUserId, OfferMessageType type, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cursor-paginated message page. cursorSentAt = boundary SentAt from the previous page (or null for the first call).
    /// olderDirection=true returns messages BEFORE the cursor (typical scroll-up); false returns messages AFTER.
    /// </summary>
    Task<ConversationMessagesPageDto> GetMessagesPageAsync(
        int conversationId,
        DateTime? cursorSentAt,
        int take,
        bool olderDirection,
        CancellationToken cancellationToken = default);

    /// <summary>Sets the caller's LastReadAt timestamp.</summary>
    Task MarkReadAsync(int conversationId, ConversationCaller caller, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve who the calling user is in the context of this conversation (teacher / student / guardian) for
    /// authorization. Returns null when the user has no claim on this conversation.
    /// </summary>
    Task<ConversationParticipantInfo?> ResolveParticipantAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
}

public enum ConversationCaller
{
    Teacher = 1,
    Student = 2
}

public record ConversationParticipantInfo(
    int ConversationId,
    int SessionRequestId,
    int TeacherId,
    int? SessionOfferId,
    int TeacherUserId,
    int StudentUserId,
    int? GuardianUserId,
    ConversationCaller CallerRole);
