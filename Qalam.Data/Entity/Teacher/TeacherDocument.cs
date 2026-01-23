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
    /// Document verification status (Pending, Approved, Rejected)
    /// </summary>
    public DocumentVerificationStatus VerificationStatus { get; set; } = DocumentVerificationStatus.Pending;

    /// <summary>
    /// Id of the admin who reviewed the document
    /// </summary>
    public int? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    // Identity Document Fields
    [MaxLength(50)]
    public string? DocumentNumber { get; set; }

    public IdentityType? IdentityType { get; set; }

    [MaxLength(3)]  // ISO 3166-1 alpha-3
    public string? IssuingCountryCode { get; set; }

    // Certificate Fields
    [MaxLength(200)]
    public string? CertificateTitle { get; set; }

    [MaxLength(200)]
    public string? Issuer { get; set; }

    public DateOnly? IssueDate { get; set; }

    // Admin Review
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    // Navigation
    public Teacher Teacher { get; set; } = null!;
}
