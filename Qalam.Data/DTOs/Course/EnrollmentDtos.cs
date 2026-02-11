using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Course;

/// <summary>
/// DTO for student to request enrollment in a course. RequestedByStudentId from auth.
/// </summary>
public class CreateEnrollmentRequestDto
{
    public int CourseId { get; set; }
    public int TeachingModeId { get; set; }
    public string? Notes { get; set; }
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
