using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Course;

/// <summary>
/// DTO for requesting enrollment in a course. UserId from auth.
/// </summary>
public class CreateEnrollmentRequestDto
{
    /// <summary>
    /// Learners to enroll that the caller owns (self and/or guardian children).
    /// Omit or send an empty list to enroll only the authenticated user (resolved server-side from their student profile).
    /// Send explicit ids when enrolling only children, or yourself together with children.
    /// </summary>
    public List<int> StudentIds { get; set; } = new();
    public int CourseId { get; set; }
    public string? Notes { get; set; }
    /// <summary>
    /// One entry per course session: concrete date and teacher weekly slot (from availability API).
    /// </summary>
    public List<SelectedSessionSlotDto> SelectedSessionSlots { get; set; } = new();
    /// <summary>
    /// الطلاب المدعوين (يحتاجون قبول الدعوة)
    /// </summary>
    public List<int> InvitedStudentIds { get; set; } = new();
    /// <summary>
    /// Optional per-session outline (duration, titles) for flexible courses. When omitted, the server derives
    /// duration from each chosen availability time slot and does not require a fixed session count beyond the calendar picks.
    /// </summary>
    public List<CreateProposedSessionDto> ProposedSessions { get; set; } = new();

    /// <summary>
    /// Optional preferred first session date. When omitted, the server uses today (UTC).
    /// </summary>
    public DateOnly? PreferredStartDate { get; set; }

    /// <summary>
    /// Optional latest date to complete all sessions. When omitted, submit-time scheduling
    /// does not enforce an end window; a default range is stored for payment/detail flows.
    /// </summary>
    public DateOnly? PreferredEndDate { get; set; }
}

public class SelectedSessionSlotDto
{
    public int SessionNumber { get; set; }
    public int TeacherAvailabilityId { get; set; }
    public DateOnly Date { get; set; }
}

/// <summary>
/// Concrete schedule slot (date + slot + duration) for each session.
/// </summary>
public class ProposedScheduleSlotDto
{
    public int SessionNumber { get; set; }
    public DateOnly Date { get; set; }
    public int TeacherAvailabilityId { get; set; }
    public int DurationMinutes { get; set; }
    public string? Title { get; set; }
}

public class CreateProposedSessionDto
{
    public int SessionNumber { get; set; }
    public int DurationMinutes { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
}

public class EnrollmentRequestProposedSessionDto
{
    public int SessionNumber { get; set; }
    public int DurationMinutes { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
}

public class EnrollmentRequestGroupMemberDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public GroupMemberType MemberType { get; set; }
    public GroupMemberConfirmationStatus ConfirmationStatus { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int? ConfirmedByUserId { get; set; }
}

public class RespondToGroupEnrollmentInviteDto
{
    public int StudentId { get; set; }
    public GroupMemberConfirmationStatus Decision { get; set; }
}

/// <summary>
/// Enrollment request list item (my requests).
/// </summary>
public class EnrollmentRequestListItemDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    public DateOnly PreferredStartDate { get; set; }
    public DateOnly PreferredEndDate { get; set; }
}

/// <summary>
/// Enrollment request detail (single request with course summary).
/// </summary>
public class EnrollmentRequestDetailDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public string? CourseDescriptionShort { get; set; }
    public decimal CoursePrice { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public int SessionTypeId { get; set; }
    public string? SessionTypeNameEn { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    public int TotalMinutes { get; set; }
    public decimal EstimatedTotalPrice { get; set; }
    public List<SelectedSessionSlotDto> SelectedSessionSlots { get; set; } = new();
    public List<EnrollmentRequestGroupMemberDto> GroupMembers { get; set; } = new();
    public List<EnrollmentRequestProposedSessionDto> ProposedSessions { get; set; } = new();
    public DateOnly PreferredStartDate { get; set; }
    public DateOnly PreferredEndDate { get; set; }
    /// <summary>Concrete schedule dates for this request (computed on read).</summary>
    public List<ProposedScheduleSlotDto> ProposedScheduleDates { get; set; } = new();
}

/// <summary>
/// My enrollment list item.
/// </summary>
public class EnrollmentListItemDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime ApprovedAt { get; set; }
    public string? TeacherDisplayName { get; set; }
}

/// <summary>
/// My enrollment detail (full enrollment + course + teacher).
/// </summary>
public class EnrollmentDetailDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public string? CourseDescription { get; set; }
    public decimal CoursePrice { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime ApprovedAt { get; set; }
    public string? TeacherDisplayName { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public int SessionTypeId { get; set; }
    public string? SessionTypeNameEn { get; set; }
}

/// <summary>
/// Minimal student info for group enrollment search results.
/// </summary>
public class StudentSearchResultDto
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = default!;
}

/// <summary>
/// Pending invitation for a student to join a group enrollment request.
/// </summary>
public class StudentInvitationListItemDto
{
    public int InvitationId { get; set; }
    public int EnrollmentRequestId { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public int InvitedStudentId { get; set; }
    public string? InvitedStudentName { get; set; }
    public string? RequestedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public GroupMemberConfirmationStatus ConfirmationStatus { get; set; }
}
