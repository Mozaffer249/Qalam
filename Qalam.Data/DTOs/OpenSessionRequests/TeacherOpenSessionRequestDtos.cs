using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.OpenSessionRequests;

// ============================================================================
// AVAILABLE REQUESTS — what the teacher sees in their inbox
// ============================================================================

/// <summary>Item in GET /Api/V1/Teacher/AvailableRequests — slim card for the inbox list.</summary>
public class TeacherAvailableRequestListItemDto
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectNameEn { get; set; }
    public string? SubjectNameAr { get; set; }
    public int? LevelId { get; set; }
    public string? LevelNameEn { get; set; }
    public string? LevelNameAr { get; set; }
    public int StudentId { get; set; }
    public string? StudentDisplayName { get; set; }
    public int SessionsCount { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public OfferGroupType? GroupType { get; set; }
    public List<DateOnly> PreferredDates { get; set; } = new();
    public int CurrentOffersCount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public OpenSessionRequestTargetStatus TargetStatus { get; set; }
    public DateTime MatchedAt { get; set; }
    public DateTime? ViewedAt { get; set; }
}

/// <summary>Full detail for GET /Api/V1/Teacher/AvailableRequests/{id}.</summary>
public class TeacherAvailableRequestDetailDto
{
    public int Id { get; set; }
    public OpenSessionRequestStatus Status { get; set; }

    public RequestContentDto Content { get; set; } = new();
    public RequestGeneralSettingsDto GeneralSettings { get; set; } = new();
    public List<TeacherViewSessionDto> Sessions { get; set; } = new();
    public RequestStudentSummaryDto Student { get; set; } = new();

    public int CurrentOffersCount { get; set; }
    /// <summary>The calling teacher's existing offer status on this request (null = none yet).</summary>
    public OpenSessionOfferStatus? MyOfferStatus { get; set; }
    public int? MyOfferId { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime PublishedAt { get; set; }
}

public class RequestContentDto
{
    public int DomainId { get; set; }
    public string? DomainNameEn { get; set; }
    public string? DomainNameAr { get; set; }
    public int? CurriculumId { get; set; }
    public string? CurriculumNameEn { get; set; }
    public int? LevelId { get; set; }
    public string? LevelNameEn { get; set; }
    public int? GradeId { get; set; }
    public string? GradeNameEn { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectNameEn { get; set; }
    public string? SubjectNameAr { get; set; }
}

public class RequestGeneralSettingsDto
{
    public int SessionsCount { get; set; }
    public int? DefaultDurationMinutes { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public OfferGroupType? GroupType { get; set; }
    public string? StudentNotes { get; set; }
}

public class TeacherViewSessionDto
{
    public int Id { get; set; }
    public int SequenceNumber { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public int? TimeSlotId { get; set; }
    public string? TimeSlotLabelEn { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public List<TeacherViewSessionUnitDto> Units { get; set; } = new();
}

public class TeacherViewSessionUnitDto
{
    public int Id { get; set; }
    public int? ContentUnitId { get; set; }
    public string? ContentUnitNameEn { get; set; }
    public string? ContentUnitNameAr { get; set; }
    public int? LessonId { get; set; }
    public string? LessonNameEn { get; set; }
    public string? LessonNameAr { get; set; }
}

public class RequestStudentSummaryDto
{
    public int Id { get; set; }
    public string? DisplayName { get; set; }
}

// ============================================================================
// AVAILABILITY MATCH
// ============================================================================

public enum SessionAvailabilityStatus
{
    /// <summary>Session falls within the teacher's TeacherAvailability and has no conflict.</summary>
    Available = 1,
    /// <summary>Session is in availability but the teacher already has a ScheduledSession or CourseSchedule at that time.</summary>
    Conflict = 2,
    /// <summary>Session is outside the teacher's TeacherAvailability (DayOfWeek + TimeSlot not covered).</summary>
    OutsideAvailability = 3
}

public class SessionAvailabilityMatchDto
{
    public int SessionId { get; set; }
    public int SequenceNumber { get; set; }
    public DateOnly PreferredDate { get; set; }
    public int TimeSlotId { get; set; }
    public SessionAvailabilityStatus Status { get; set; }
    /// <summary>When Status = Conflict, a free-text label identifying what is in the way.</summary>
    public string? ConflictWith { get; set; }
}

// ============================================================================
// OFFERS — bodies + responses
// ============================================================================

public class CreateSessionOfferDto
{
    public int SessionRequestId { get; set; }
    public decimal Price { get; set; }
    public string? TeacherNotes { get; set; }
    /// <summary>Between 24 and 168 (1 to 7 days). Default 48 if omitted.</summary>
    public int ValidityHours { get; set; } = 48;
    /// <summary>Must be true — teacher confirms they will honor the proposed schedule.</summary>
    public bool CommitmentConfirmed { get; set; }
}

public class UpdateSessionOfferDto
{
    public decimal? Price { get; set; }
    public string? TeacherNotes { get; set; }
    public int? ValidityHours { get; set; }
}

public class WithdrawSessionOfferDto
{
    public string? Reason { get; set; }
}

public class TeacherOfferListItemDto
{
    public int Id { get; set; }
    public int SessionRequestId { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectNameEn { get; set; }
    public string? SubjectNameAr { get; set; }
    public int StudentId { get; set; }
    public string? StudentDisplayName { get; set; }
    public decimal Price { get; set; }
    public int SessionsCount { get; set; }
    public OpenSessionOfferStatus Status { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int UnreadMessagesCount { get; set; }
}

public class TeacherOfferDetailDto
{
    public int Id { get; set; }
    public int SessionRequestId { get; set; }
    public int TeacherId { get; set; }
    public decimal Price { get; set; }
    public string? TeacherNotes { get; set; }
    public OpenSessionOfferStatus Status { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public string? RejectionReason { get; set; }
    public int? ConversationId { get; set; }
    /// <summary>Quick reference back to the request so the UI can render context without an extra fetch.</summary>
    public TeacherAvailableRequestDetailDto? Request { get; set; }
}

// Response payload when POST /Teacher/Offers conflicts because the teacher already has a non-Withdrawn offer.
public class DuplicateOfferMetaDto
{
    public int ExistingOfferId { get; set; }
    public OpenSessionOfferStatus ExistingOfferStatus { get; set; }
}

// ============================================================================
// CONVERSATIONS (chat between the offering teacher and the requesting student/guardian)
// ============================================================================

public class OfferConversationDto
{
    public int ConversationId { get; set; }
    public int OfferId { get; set; }
    public List<ConversationParticipantDto> Participants { get; set; } = new();
    public DateTime? LastMessageAt { get; set; }
    /// <summary>Unread count from the caller's perspective.</summary>
    public int UnreadCount { get; set; }
}

public class ConversationParticipantDto
{
    public int UserId { get; set; }
    public string? DisplayName { get; set; }
    /// <summary>"Teacher" or "Student".</summary>
    public string? Role { get; set; }
}

public class ConversationMessagesPageDto
{
    public List<OfferConversationMessageDto> Messages { get; set; } = new();
    /// <summary>ISO-8601 timestamp of the oldest message in this page; pass back as `cursor` to fetch the next page going older.</summary>
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
}

public class OfferConversationMessageDto
{
    public int Id { get; set; }
    public OfferMessageType Type { get; set; }
    public int? SenderUserId { get; set; }
    public string? SenderDisplayName { get; set; }
    public string? SenderRole { get; set; }
    public string Content { get; set; } = default!;
    public DateTime SentAt { get; set; }
}

public class PostConversationMessageDto
{
    public string Content { get; set; } = default!;
}

public class MarkConversationReadDto
{
    public int? UpToMessageId { get; set; }
}
