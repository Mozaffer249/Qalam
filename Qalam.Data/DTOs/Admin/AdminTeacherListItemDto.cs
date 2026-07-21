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
}
