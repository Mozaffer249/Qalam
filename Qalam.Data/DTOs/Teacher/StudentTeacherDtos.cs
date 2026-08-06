using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class StudentTeacherProfileDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string FullName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public decimal RatingAverage { get; set; }
    public int ReviewsCount { get; set; }
    public TeacherLocation? Location { get; set; }
    public int StudentsCount { get; set; }
    public int CoursesCount { get; set; }
    public int SubjectsCount { get; set; }
    public int SessionsCount { get; set; }
    public List<TeacherCardSubjectDto> Subjects { get; set; } = new();
    public List<StudentTeacherReviewDto> Reviews { get; set; } = new();
    public List<StudentTeacherCertificateDto> Certificates { get; set; } = new();
    public List<CourseCatalogIndexItemDto> Courses { get; set; } = new();
}

public class StudentTeacherSubjectDto
{
    /// <summary>TeacherSubject.Id — use with Units endpoint for repertoire-scoped content.</summary>
    public int TeacherSubjectId { get; set; }
    public int SubjectId { get; set; }
    public string SubjectNameAr { get; set; } = default!;
    public string SubjectNameEn { get; set; } = default!;
    public int? DomainId { get; set; }
    public string? DomainCode { get; set; }
    public string? DomainNameAr { get; set; }
    public string? DomainNameEn { get; set; }
    public string? GradeNameAr { get; set; }
    public string? GradeNameEn { get; set; }
    public string? LevelNameAr { get; set; }
    public string? LevelNameEn { get; set; }
    public string? CurriculumNameAr { get; set; }
    public string? CurriculumNameEn { get; set; }
    public bool CanTeachFullSubject { get; set; }
    public int UnitsCount { get; set; }
    public List<StudentTeacherSubjectUnitDto> Units { get; set; } = new();
}

public class StudentTeacherSubjectUnitDto
{
    public int UnitId { get; set; }
    public string UnitNameAr { get; set; } = default!;
    public string UnitNameEn { get; set; } = default!;
    public string? UnitTypeCode { get; set; }
}

public class StudentTeacherReviewDto
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Feedback { get; set; }
    public string? StudentDisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StudentTeacherCertificateDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Issuer { get; set; }
    public DateOnly? IssueDate { get; set; }
    public string FileUrl { get; set; } = default!;
}
