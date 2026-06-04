using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.OpenSessionRequests;

// ============================================================================
// Create — the student (or their guardian) submits a single payload.
// ============================================================================

/// <summary>
/// Body for POST Api/V1/Student/OpenSessionRequests — one-shot create+publish.
/// </summary>
public class CreateOpenSessionRequestDto
{
    /// <summary>
    /// The learner the request is for. When the caller is the student themselves, this is their own Student.Id.
    /// When a guardian creates on behalf of a minor, this is the child Student.Id.
    /// </summary>
    public int StudentId { get; set; }

    public int DomainId { get; set; }
    public int SubjectId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    public int? TermId { get; set; }
    public int TeachingModeId { get; set; }

    /// <summary>
    /// Optional — target a single specific teacher instead of broadcasting to all matched teachers.
    /// When set, the server validates that the teacher offers the chosen <see cref="SubjectId"/>
    /// (active TeacherSubject row) and that every per-session <c>Units[]</c> entry belongs to that
    /// teacher's TeacherSubjectUnits. The broadcast matching algorithm is skipped — only this teacher
    /// receives a Target row + notification.
    /// </summary>
    public int? TargetedTeacherId { get; set; }

    /// <summary>
    /// Open group / Invite-only. Required when TeachingMode resolves to "Group".
    /// </summary>
    public OfferGroupType? GroupType { get; set; }

    public int TotalSessionsCount { get; set; }
    public string? StudentNotes { get; set; }

    /// <summary>
    /// Optional. Defaults to PublishedAt + 7 days.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    public List<CreateOpenSessionRequestSessionDto> Sessions { get; set; } = new();

    /// <summary>
    /// Student.Ids to invite as co-learners. Max 5; allowed only for Group teaching modes.
    /// </summary>
    public List<int> InvitedStudentIds { get; set; } = new();
}

public class CreateOpenSessionRequestSessionDto
{
    public int SequenceNumber { get; set; }
    public DateOnly PreferredDate { get; set; }
    public int TimeSlotId { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public int? QuranContentTypeId { get; set; }
    public int? QuranLevelId { get; set; }
    public string? Notes { get; set; }
    public List<CreateOpenSessionRequestUnitDto> Units { get; set; } = new();
}

public class CreateOpenSessionRequestUnitDto
{
    /// <summary>Exactly one of ContentUnitId or LessonId must be set.</summary>
    public int? ContentUnitId { get; set; }
    public int? LessonId { get; set; }

    /// <summary>
    /// Only meaningful when <see cref="ContentUnitId"/> is set.
    /// <c>true</c>  → the row covers every lesson inside the unit.
    /// <c>false</c> → the row is "this unit as a topic header" — no specific lessons committed (default).
    /// Setting <c>true</c> together with <see cref="LessonId"/> is rejected — single-lesson rows can't expand.
    /// </summary>
    public bool IncludesAllLessons { get; set; }
}

// ============================================================================
// Cancel
// ============================================================================

public class CancelOpenSessionRequestDto
{
    public string? Reason { get; set; }
}

// ============================================================================
// Invitation response
// ============================================================================

public class RespondToOpenSessionRequestInvitationDto
{
    /// <summary>The invited Student.Id whose invitation is being answered.</summary>
    public int StudentId { get; set; }

    /// <summary>Accepted or Rejected (Pending/Expired are not valid here).</summary>
    public OpenSessionRequestInvitationStatus Decision { get; set; }
}

// ============================================================================
// Detail response
// ============================================================================

public class OpenSessionRequestDetailDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int RequestedByUserId { get; set; }
    public int? CreatedByGuardianId { get; set; }
    public string? CreatedByGuardianName { get; set; }

    public int DomainId { get; set; }
    public string? DomainName { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    public int? TermId { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeName { get; set; }
    public OfferGroupType? GroupType { get; set; }
    public int TotalSessionsCount { get; set; }

    public OpenSessionRequestStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? StudentNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<OpenSessionRequestSessionDto> Sessions { get; set; } = new();
    public List<OpenSessionRequestInvitationDto> Invitations { get; set; } = new();
    public List<OpenSessionRequestAttachmentDto> Attachments { get; set; } = new();
    public int OffersCount { get; set; }
}

public class OpenSessionRequestSessionDto
{
    public int Id { get; set; }
    public int SequenceNumber { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public int? TimeSlotId { get; set; }
    public int DurationMinutes { get; set; }
    public int? QuranContentTypeId { get; set; }
    public int? QuranLevelId { get; set; }
    public string? Notes { get; set; }
    public List<OpenSessionRequestUnitDto> Units { get; set; } = new();
}

public class OpenSessionRequestUnitDto
{
    public int Id { get; set; }
    public int? ContentUnitId { get; set; }
    public int? LessonId { get; set; }
}

public class OpenSessionRequestInvitationDto
{
    public int Id { get; set; }
    public int InvitedStudentId { get; set; }
    public string? InvitedStudentName { get; set; }
    public int InvitedByStudentId { get; set; }
    public OpenSessionRequestInvitationStatus Status { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class OpenSessionRequestAttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? PublicUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================================
// List item (for GET /my)
// ============================================================================

public class OpenSessionRequestListItemDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public int TeachingModeId { get; set; }
    public OpenSessionRequestStatus Status { get; set; }
    public int TotalSessionsCount { get; set; }
    public int OffersCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
