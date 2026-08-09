using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Explicit admin approval of an education domain for a teacher.
/// Active when <see cref="RevokedAt"/> is null. At least one active approval
/// is required before the teacher account can be authorized.
/// </summary>
public class TeacherDomainApproval : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int DomainId { get; set; }

    public int? ApprovedByAdminId { get; set; }

    public DateTime ApprovedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public int? RevokedByAdminId { get; set; }

    [MaxLength(500)]
    public string? RevokeReason { get; set; }

    public Teacher Teacher { get; set; } = null!;
    public EducationDomain Domain { get; set; } = null!;
}
