using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// المواد التي يدرسها المعلم
/// </summary>
public class TeacherSubject : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int SubjectId { get; set; }

    /// <summary>
    /// هل يمكنه تدريس المادة كاملة؟
    /// </summary>
    public bool CanTeachFullSubject { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DocumentVerificationStatus VerificationStatus { get; set; } = DocumentVerificationStatus.Approved;

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public int? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    // Navigation Properties
    public Teacher Teacher { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<TeacherSubjectUnit> TeacherSubjectUnits { get; set; } = new List<TeacherSubjectUnit>();
}
