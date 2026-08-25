using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Admin;

/// <summary>Row shape for admin paginated student browse.</summary>
public class AdminStudentListItemDto
{
    public int StudentId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsMinor { get; set; }
    public bool IsActive { get; set; }
    public string? GuardianName { get; set; }
    public int ChildrenCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Guardian block on an admin student file (when the student is a minor).</summary>
public class AdminStudentGuardianDto
{
    public int GuardianId { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public GuardianRelation? Relation { get; set; }
}

/// <summary>
/// Child linked via the viewed user's guardian profile.
/// Field set mirrors <see cref="Qalam.Data.DTOs.Student.ChildStudentDto"/> (admin-scoped, no session enrich).
/// </summary>
public class AdminStudentChildDto
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = "";
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public GuardianRelation? GuardianRelation { get; set; }
    public int? DomainId { get; set; }
    public string? DomainNameEn { get; set; }
    public string? DomainNameAr { get; set; }
    public int? CurriculumId { get; set; }
    public string? CurriculumNameEn { get; set; }
    public string? CurriculumNameAr { get; set; }
    public int? LevelId { get; set; }
    public string? LevelNameEn { get; set; }
    public string? LevelNameAr { get; set; }
    public int? GradeId { get; set; }
    public string? GradeNameEn { get; set; }
    public string? GradeNameAr { get; set; }
    public bool IsActive { get; set; }
    public bool IsSelf { get; set; }
}

/// <summary>Admin student file / detail.</summary>
public class AdminStudentDetailDto
{
    public int StudentId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsMinor { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? DomainId { get; set; }
    public string? DomainNameEn { get; set; }
    public string? DomainNameAr { get; set; }
    public int? CurriculumId { get; set; }
    public string? CurriculumNameEn { get; set; }
    public string? CurriculumNameAr { get; set; }
    public int? LevelId { get; set; }
    public string? LevelNameEn { get; set; }
    public string? LevelNameAr { get; set; }
    public int? GradeId { get; set; }
    public string? GradeNameEn { get; set; }
    public string? GradeNameAr { get; set; }

    public AdminStudentGuardianDto? Guardian { get; set; }
    public List<AdminStudentChildDto> Children { get; set; } = [];
}
