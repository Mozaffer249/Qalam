using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// A teacher's one-time answer for a domain question.
/// </summary>
public class TeacherDomainQuestionSubmission : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int QuestionId { get; set; }

    public DocumentVerificationStatus VerificationStatus { get; set; } = DocumentVerificationStatus.Pending;

    [MaxLength(2000)]
    public string? TextValue { get; set; }

    public bool? BoolValue { get; set; }

    public int? TeacherDocumentId { get; set; }

    public int? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public Teacher Teacher { get; set; } = null!;
    public TeacherDomainQuestion Question { get; set; } = null!;
    public TeacherDocument? TeacherDocument { get; set; }
}
