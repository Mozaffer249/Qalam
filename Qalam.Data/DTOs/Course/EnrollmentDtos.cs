using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Course;

/// <summary>
/// DTO for student to request enrollment in a course. RequestedByStudentId from auth.
/// </summary>
public class CreateEnrollmentRequestDto
{
    /// <summary>
    /// Target student ID. If null, the logged-in user's own student profile is used.
    /// Guardians must provide their child's StudentId.
    /// </summary>
    public int? StudentId { get; set; }
    public int CourseId { get; set; }
    public int TeachingModeId { get; set; }
    public string? Notes { get; set; }
    public List<int> SelectedAvailabilityIds { get; set; } = new();
    public List<int> GroupMemberStudentIds { get; set; } = new();
    public List<CreateProposedSessionDto> ProposedSessions { get; set; } = new();
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
    public List<int> SelectedAvailabilityIds { get; set; } = new();
    public List<EnrollmentRequestGroupMemberDto> GroupMembers { get; set; } = new();
    public List<EnrollmentRequestProposedSessionDto> ProposedSessions { get; set; } = new();
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
