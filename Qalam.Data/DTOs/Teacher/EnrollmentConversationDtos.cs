using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class EnrollmentConversationDto
{
    public int ConversationId { get; set; }
    public int EnrollmentId { get; set; }
    public List<EnrollmentConversationParticipantDto> Participants { get; set; } = new();
    public DateTime? LastMessageAt { get; set; }
    /// <summary>Unread count from the caller's perspective.</summary>
    public int UnreadCount { get; set; }
}

public class EnrollmentConversationParticipantDto
{
    public int UserId { get; set; }
    public string? DisplayName { get; set; }
    /// <summary>"Teacher" or "Student".</summary>
    public string? Role { get; set; }
}

public class EnrollmentConversationMessagesPageDto
{
    public List<EnrollmentConversationMessageDto> Messages { get; set; } = new();
    /// <summary>ISO-8601 timestamp of the oldest message in this page; pass back as `cursor` to fetch the next page going older.</summary>
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
}

public class EnrollmentConversationMessageDto
{
    public int Id { get; set; }
    public EnrollmentMessageType Type { get; set; }
    public int? SenderUserId { get; set; }
    public string? SenderDisplayName { get; set; }
    public string? SenderRole { get; set; }
    public string Content { get; set; } = default!;
    public DateTime SentAt { get; set; }
}

public class PostEnrollmentConversationMessageDto
{
    public string Content { get; set; } = default!;
}

public class MarkEnrollmentConversationReadDto
{
    public int? UpToMessageId { get; set; }
}
