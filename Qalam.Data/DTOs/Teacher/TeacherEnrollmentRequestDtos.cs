using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class TeacherEnrollmentRequestListItemDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public string? RequestedByUserName { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalMinutes { get; set; }
    public decimal EstimatedTotalPrice { get; set; }
    public int GroupMemberCount { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public string? SessionTypeNameEn { get; set; }
}

public class TeacherEnrollmentRequestDetailDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public string? RequestedByUserName { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalMinutes { get; set; }
    public decimal EstimatedTotalPrice { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public string? SessionTypeNameEn { get; set; }
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public List<int> SelectedAvailabilityIds { get; set; } = new();
    public List<TeacherEnrollmentRequestGroupMemberDto> GroupMembers { get; set; } = new();
    public List<TeacherEnrollmentRequestProposedSessionDto> ProposedSessions { get; set; } = new();
}

public class TeacherEnrollmentRequestGroupMemberDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public GroupMemberType MemberType { get; set; }
    public GroupMemberConfirmationStatus ConfirmationStatus { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class TeacherEnrollmentRequestProposedSessionDto
{
    public int SessionNumber { get; set; }
    public int DurationMinutes { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
}

public class RejectEnrollmentRequestDto
{
    public string? RejectionReason { get; set; }
}
