using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

/// <summary>
/// Lightweight teacher card returned by the student-facing browse / recommended endpoints.
/// The <see cref="Id"/> plugs straight back into <c>CreateOpenSessionRequestDto.TargetedTeacherId</c>
/// for the Scenario 2 targeted-teacher flow.
/// </summary>
public class TeacherCardDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string FullName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public decimal RatingAverage { get; set; }
    public int ReviewsCount { get; set; }
    public TeacherLocation? Location { get; set; }

    /// <summary>Subjects this teacher offers (up to 5 — preview only).</summary>
    public List<TeacherCardSubjectDto> Subjects { get; set; } = new();
}

/// <summary>One subject row on a teacher card. Lightweight — no nested units/lessons.</summary>
public class TeacherCardSubjectDto
{
    public int SubjectId { get; set; }
    public string? SubjectNameAr { get; set; }
    public string? SubjectNameEn { get; set; }
    public int? DomainId { get; set; }
    public string? DomainCode { get; set; }
    public bool CanTeachFullSubject { get; set; }
    public int UnitsCount { get; set; }
}
