using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Admin;

/// <summary>
/// Row shape for admin paginated teacher browse (all statuses).
/// </summary>
public class AdminTeacherListItemDto
{
    public int TeacherId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string Status { get; set; } = null!;
    public TeacherLocation? Location { get; set; }
    public string? Nationality { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }

    /// <summary>Distinct domain codes from domain-question submissions and teacher subjects.</summary>
    public string SelectedDomainCodes { get; set; } = "";

    public string SelectedDomainNamesAr { get; set; } = "";
    public string SelectedDomainNamesEn { get; set; } = "";
    public string SubjectNamesAr { get; set; } = "";
    public string SubjectNamesEn { get; set; } = "";
    public string CertificateTitles { get; set; } = "";

    /// <summary>Compact Q&amp;A summary for the browse table.</summary>
    public string DomainAnswersSummary { get; set; } = "";

    /// <summary>Full domain-question groups (answers) for export / detail-lite.</summary>
    public List<TeacherDomainQuestionGroupDto> DomainQuestionSubmissions { get; set; } = new();

    /// <summary>Per-requirement registration checklist (same shape as teacher detail).</summary>
    public List<TeacherRegistrationSubmissionStatusDto> RegistrationRequirements { get; set; } = new();
}

/// <summary>CSV file payload for admin teacher export.</summary>
public class AdminTeacherCsvExportDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "teachers-export.csv";
}
