using Qalam.Data.DTOs.Teacher;

namespace Qalam.Data.DTOs.Admin;

public class AdminTeacherSubjectDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string TeacherFullName { get; set; } = null!;
    public int SubjectId { get; set; }
    public string SubjectNameAr { get; set; } = null!;
    public string SubjectNameEn { get; set; } = null!;
    public string? DomainCode { get; set; }
    public bool CanTeachFullSubject { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> QuranContentTypeIds { get; set; } = new();
    public List<int> QuranLevelIds { get; set; } = new();
    public List<int> WritableFilterValueIds { get; set; } = new();
    public List<TeacherSubjectUnitResponseDto> Units { get; set; } = new();
}

public class TeacherSubjectSummaryDto
{
    public int TotalSubjects { get; set; }
    public int ActiveSubjects { get; set; }
    public int InactiveSubjects { get; set; }
}

public class TeacherSubjectActivationSnapshot
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
}
