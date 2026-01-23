using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Admin;

public class TeacherDetailsDto
{
    public int TeacherId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public TeacherStatus Status { get; set; }
    public TeacherLocation? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<TeacherDocumentReviewDto> Documents { get; set; } = new();
    
    // Summary
    public int TotalDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public bool CanBeActivated => PendingDocuments == 0 && RejectedDocuments == 0 && TotalDocuments > 0;
}
