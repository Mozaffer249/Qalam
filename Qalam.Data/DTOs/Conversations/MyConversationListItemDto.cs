using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Data.DTOs.Conversations;

public class MyConversationListItemDto
{
    public int ConversationId { get; set; }

    /// <summary>"Offer" or "Enrollment".</summary>
    public string ConversationType { get; set; } = "Offer";

    public string? TeacherName { get; set; }

    public string? SubjectName { get; set; }

    public string? LastMessageContent { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public int UnreadCount { get; set; }

    /// <summary>OfferId when ConversationType = Offer (for navigation). Null for a targeted thread where the teacher is chatting before sending an offer.</summary>
    public int? OfferId { get; set; }

    /// <summary>OSR request id when ConversationType = Offer — lets the client open the thread via by-request even when OfferId is still null.</summary>
    public int? RequestId { get; set; }

    /// <summary>Teacher id on the OSR thread; pairs with <see cref="RequestId"/> for the by-request navigation fallback.</summary>
    public int? TeacherId { get; set; }

    /// <summary>EnrollmentId when ConversationType = Enrollment (for navigation).</summary>
    public int? EnrollmentId { get; set; }

    public List<ConversationParticipantDto> Participants { get; set; } = new();
}
