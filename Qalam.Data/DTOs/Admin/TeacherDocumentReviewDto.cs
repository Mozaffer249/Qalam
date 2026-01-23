using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Admin;

public class TeacherDocumentReviewDto
{
    public int Id { get; set; }
    public TeacherDocumentType DocumentType { get; set; }
    public string FilePath { get; set; } = null!;
    public DocumentVerificationStatus VerificationStatus { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    
    // Identity Document Fields
    public string? DocumentNumber { get; set; }
    public IdentityType? IdentityType { get; set; }
    public string? IssuingCountryCode { get; set; }
    
    // Certificate Fields
    public string? CertificateTitle { get; set; }
    public string? Issuer { get; set; }
    public DateOnly? IssueDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
