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

    /// <summary>
    /// Optional teacher-subject scope for per-session content units / lessons.
    /// When set, must equal <c>Course.TeacherSubjectId</c>; each <c>Units</c> row in <see cref="SelectedSessionSlots"/>
    /// and <see cref="ProposedSessions"/> is hard-validated against the teacher's <c>TeacherSubjectUnits</c> repertoire.
    /// When null, units are accepted as long as the FK exists in <c>ContentUnits</c> / <c>Lessons</c> (free-form).
    /// </summary>
    public int? TeacherSubjectId { get; set; }

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

    /// <summary>
    /// Content units / lessons covered in this session. Each row must set exactly one of
    /// <see cref="EnrollmentSessionUnitDto.ContentUnitId"/> or <see cref="EnrollmentSessionUnitDto.LessonId"/>.
    /// Same shape scenario 2 uses on <c>OpenSessionRequestSessionUnit</c>.
    /// </summary>
    public List<EnrollmentSessionUnitDto> Units { get; set; } = new();
}

/// <summary>
/// One content unit OR lesson covered in a session of a course enrollment request.
/// Exactly one of <see cref="ContentUnitId"/> or <see cref="LessonId"/> must be set.
/// </summary>
public class EnrollmentSessionUnitDto
{
    public int? ContentUnitId { get; set; }
    public int? LessonId { get; set; }

    /// <summary>Resolved name in response payloads. Ignored on request.</summary>
    public string? ContentUnitNameEn { get; set; }
    /// <summary>Resolved name in response payloads. Ignored on request.</summary>
    public string? LessonNameEn { get; set; }
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

    /// <summary>
    /// Content units / lessons covered in this proposed session. Same validation rules as
    /// <see cref="SelectedSessionSlotDto.Units"/>.
    /// </summary>
    public List<EnrollmentSessionUnitDto> Units { get; set; } = new();
}

public class EnrollmentRequestProposedSessionDto
{
    public int SessionNumber { get; set; }
    public int DurationMinutes { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }

    /// <summary>Content units / lessons covered in this session, with resolved names.</summary>
    public List<EnrollmentSessionUnitDto> Units { get; set; } = new();
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
    /// <summary>
    /// Pending/approval request row id (<c>CourseEnrollmentRequest</c>). Do not use this for payment;
    /// after approval, call GET Student/Enrollments and use <see cref="EnrollmentListItemDto.Id"/> as <c>enrollmentId</c>.
    /// </summary>
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

    /// <summary>True when any Invited member is still Pending.</summary>
    public bool HasPendingInvites { get; set; }

    /// <summary>Linked enrollment id when artifacts exist.</summary>
    public int? EnrollmentId { get; set; }

    public EnrollmentStatus? EnrollmentStatus { get; set; }
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

    /// <summary>True when the caller is the request owner (RequestedByUserId).</summary>
    public bool IsOwner { get; set; }

    /// <summary>Individual or Group from course session type / enrollment.</summary>
    public EnrollmentKind Kind { get; set; }

    /// <summary>Owner may pay full AmountDue when enrollment is PendingPayment.</summary>
    public bool CanPay { get; set; }

    /// <summary>Owner may cancel pending Invited members while enrollment does not exist yet.</summary>
    public bool CanCancelInvite { get; set; }

    /// <summary>Owner may cancel the whole request before pay (and linked PendingPayment enrollment).</summary>
    public bool CanCancel { get; set; }

    /// <summary>Student ids the viewer may Accept/Reject (self and/or guardian children).</summary>
    public List<int> ActionableMemberStudentIds { get; set; } = new();

    public List<int> ViewerStudentIds { get; set; } = new();

    public int? EnrollmentId { get; set; }
    public EnrollmentStatus? EnrollmentStatus { get; set; }
    public decimal? AmountDue { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    /// <summary>Any participant id — use with single-payer pay endpoint.</summary>
    public int? PayParticipantId { get; set; }
}

/// <summary>
/// My enrollment list item (unified — individual or group).
/// </summary>
public class EnrollmentListItemDto
{
    /// <summary>
    /// <see cref="Enrollment"/> primary key.
    /// </summary>
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public EnrollmentKind Kind { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime ApprovedAt { get; set; }
    public string? TeacherDisplayName { get; set; }
    public int ParticipantCount { get; set; }
    public string? LeaderStudentName { get; set; }
}

/// <summary>
/// A single participant in a unified enrollment (one row for individual, N rows for group).
/// </summary>
public class EnrollmentParticipantDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentFullName { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
}

/// <summary>
/// One persisted schedule row for GET enrollment detail (student view).
/// </summary>
public class EnrollmentSessionItemDto
{
    /// <summary>
    /// <see cref="CourseSchedule"/> id.
    /// </summary>
    public int ScheduleId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>
    /// Session outline title from the enrollment request (<c>CourseRequestProposedSession</c>) or course template (<c>CourseSession</c>), when available.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Weekly slot start time on <see cref="Date"/> (from teacher time slot). Null if availability/time slot was not loaded.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Weekly slot end time on <see cref="Date"/> (from teacher time slot).
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    public int DurationMinutes { get; set; }

    public ScheduleStatus Status { get; set; }

    /// <summary>
    /// True when enrollment is Active, status is Scheduled, and current UTC lies within the session window on that date.
    /// </summary>
    public bool CanStart { get; set; }
}

/// <summary>
/// My enrollment detail (full enrollment + course + teacher + participants).
/// </summary>
public class EnrollmentDetailDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public string? CourseDescription { get; set; }
    public decimal CoursePrice { get; set; }
    public EnrollmentKind Kind { get; set; }
    public int? LeaderStudentId { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime ApprovedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? TeacherDisplayName { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public int SessionTypeId { get; set; }
    public string? SessionTypeNameEn { get; set; }

    /// <summary>
    /// Participants in this enrollment. One row for Individual; N rows for Group.
    /// </summary>
    public List<EnrollmentParticipantDto> Participants { get; set; } = new();

    /// <summary>
    /// Scheduled sessions after payment (from <see cref="CourseSchedule"/>). Empty if none generated yet.
    /// </summary>
    public List<EnrollmentSessionItemDto> Sessions { get; set; } = new();

    /// <summary>True when the caller is the enrollment owner (OwnerUserId or legacy request owner).</summary>
    public bool IsOwner { get; set; }

    /// <summary>Owner may pay full AmountDue when enrollment is PendingPayment.</summary>
    public bool CanPay { get; set; }

    /// <summary>Owner may cancel while enrollment is PendingPayment.</summary>
    public bool CanCancel { get; set; }

    public decimal AmountDue { get; set; }
    public DateTime? PaymentDeadline { get; set; }

    /// <summary>Any pending participant id — use with single-payer pay endpoint.</summary>
    public int? PayParticipantId { get; set; }
}

/// <summary>
/// Result of POST Individual enrollment (no CourseEnrollmentRequest).
/// </summary>
public class CreateIndividualEnrollmentResultDto
{
    public int Id { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public decimal AmountDue { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public int? PayParticipantId { get; set; }
    public bool CanPay { get; set; }
    public string CourseTitle { get; set; } = default!;
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