using Qalam.Data.DTOs.Teacher;
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

    /// <summary>Active registration requirements with submission status (admin review checklist).</summary>
    public List<TeacherRegistrationSubmissionStatusDto> RegistrationRequirements { get; set; } = new();
    
    // Summary
    public int TotalDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    /// <summary>Set by API from registration checklist when configured; otherwise from document counts.</summary>
    public bool CanBeActivated { get; set; }

    public List<AdminTeacherSubjectDto> Subjects { get; set; } = new();
    public TeacherSubjectSummaryDto SubjectSummary { get; set; } = new();
}
