using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Legal;

/// <summary>Records that a user accepted a specific published legal document version.</summary>
public class UserLegalConsent : AuditableEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int LegalDocumentId { get; set; }

    public int LegalDocumentVersionId { get; set; }

    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>e.g. teacher-register, student-register, accept-terms, reaccept.</summary>
    [MaxLength(50)]
    public string? Source { get; set; }

    public User User { get; set; } = null!;
    public LegalDocument LegalDocument { get; set; } = null!;
    public LegalDocumentVersion LegalDocumentVersion { get; set; } = null!;
}
