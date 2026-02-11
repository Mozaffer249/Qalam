using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Course;

/// <summary>
/// DTO for creating a course (teacher). Status defaults to Draft on create.
/// </summary>
public class CreateCourseDto
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int TeacherSubjectId { get; set; }
    public int TeachingModeId { get; set; }
    public int SessionTypeId { get; set; }
    public bool IsFlexible { get; set; }
    public int? SessionsCount { get; set; }
    public int? SessionDurationMinutes { get; set; }
    public decimal Price { get; set; }
    public int? MaxStudents { get; set; }
    public bool CanIncludeInPackages { get; set; }
}

/// <summary>
/// DTO for updating a course. Id can be on command (route).
/// </summary>
public class UpdateCourseDto
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int TeacherSubjectId { get; set; }
    public int TeachingModeId { get; set; }
    public int SessionTypeId { get; set; }
    public bool IsFlexible { get; set; }
    public int? SessionsCount { get; set; }
    public int? SessionDurationMinutes { get; set; }
    public decimal Price { get; set; }
    public int? MaxStudents { get; set; }
    public bool CanIncludeInPackages { get; set; }
}

/// <summary>
/// Course list item with optional display names from includes.
/// </summary>
public class CourseListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string? DescriptionShort { get; set; }
    public int TeacherId { get; set; }
    public int DomainId { get; set; }
    public string? DomainNameEn { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectNameEn { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public int SessionTypeId { get; set; }
    public string? SessionTypeNameEn { get; set; }
    public CourseStatus Status { get; set; }
    public bool IsActive { get; set; }
    public decimal Price { get; set; }
}

/// <summary>
/// Full course details for GetById, with related entity names.
/// </summary>
public class CourseDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int TeacherId { get; set; }
    public string? TeacherDisplayName { get; set; }
    public int DomainId { get; set; }
    public string? DomainNameEn { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectNameEn { get; set; }
    public int? CurriculumId { get; set; }
    public string? CurriculumNameEn { get; set; }
    public int? LevelId { get; set; }
    public string? LevelNameEn { get; set; }
    public int? GradeId { get; set; }
    public string? GradeNameEn { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public int SessionTypeId { get; set; }
    public string? SessionTypeNameEn { get; set; }
    public bool IsFlexible { get; set; }
    public int? SessionsCount { get; set; }
    public int? SessionDurationMinutes { get; set; }
    public decimal Price { get; set; }
    public int? MaxStudents { get; set; }
    public bool CanIncludeInPackages { get; set; }
    public CourseStatus Status { get; set; }
}

/// <summary>
/// Student catalog list item (published courses).
/// </summary>
public class CourseCatalogItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string? DescriptionShort { get; set; }
    public string? TeacherDisplayName { get; set; }
    public int DomainId { get; set; }
    public string? DomainNameEn { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectNameEn { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public int SessionTypeId { get; set; }
    public string? SessionTypeNameEn { get; set; }
    public decimal Price { get; set; }
    public int? MaxStudents { get; set; }
    public int? AvailableSeats { get; set; }
    public bool IsFlexible { get; set; }
    public int? SessionsCount { get; set; }
    public int? SessionDurationMinutes { get; set; }
}

/// <summary>
/// Student catalog course detail (single published course page).
/// </summary>
public class CourseCatalogDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? TeacherDisplayName { get; set; }
    public int DomainId { get; set; }
    public string? DomainNameEn { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectNameEn { get; set; }
    public int? CurriculumId { get; set; }
    public string? CurriculumNameEn { get; set; }
    public int? LevelId { get; set; }
    public string? LevelNameEn { get; set; }
    public int? GradeId { get; set; }
    public string? GradeNameEn { get; set; }
    public int TeachingModeId { get; set; }
    public string? TeachingModeNameEn { get; set; }
    public int SessionTypeId { get; set; }
    public string? SessionTypeNameEn { get; set; }
    public bool IsFlexible { get; set; }
    public int? SessionsCount { get; set; }
    public int? SessionDurationMinutes { get; set; }
    public decimal Price { get; set; }
    public int? MaxStudents { get; set; }
    public int? AvailableSeats { get; set; }
}
