using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

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
    public DocumentVerificationStatus VerificationStatus { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TeacherSubjectUnitResponseDto> Units { get; set; } = new();
}

public class TeacherSubjectSummaryDto
{
    public int TotalSubjects { get; set; }
    public int ActiveSubjects { get; set; }
    public int PendingSubjects { get; set; }
    public int InactiveSubjects { get; set; }
    public int RejectedSubjects { get; set; }
}

public class TeacherSubjectActivationSnapshot
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
}
