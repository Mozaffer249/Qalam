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

    /// <summary>OfferId when ConversationType = Offer (for navigation).</summary>
    public int? OfferId { get; set; }

    /// <summary>EnrollmentId when ConversationType = Enrollment (for navigation).</summary>
    public int? EnrollmentId { get; set; }

    public List<ConversationParticipantDto> Participants { get; set; } = new();
}
