using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Teacher document entity represents a document of a teacher in the system
/// </summary>
public class TeacherDocument : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public TeacherDocumentType DocumentType { get; set; }

    [Required, MaxLength(400)]
    public string FilePath { get; set; } = null!;

    /// <summary>
    /// Whether the document has been verified
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Id of the admin who verified the document
    /// </summary>
    public int? VerifiedByAdminId { get; set; }

    public DateTime? VerifiedAt { get; set; }

    // Navigation
    public Teacher Teacher { get; set; } = null!;
}
