using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// دعوة طالب آخر للانضمام كمشارك في طلب جلسات (مجموعة بدعوة فقط).
/// </summary>
public class OpenSessionRequestInvitation : AuditableEntity
{
    public int Id { get; set; }

    public int SessionRequestId { get; set; }

    /// <summary>
    /// الطالب المدعو.
    /// </summary>
    public int InvitedStudentId { get; set; }

    /// <summary>
    /// الطالب الذي أرسل الدعوة (عادةً مالك الطلب).
    /// </summary>
    public int InvitedByStudentId { get; set; }

    public OpenSessionRequestInvitationStatus Status { get; set; } = OpenSessionRequestInvitationStatus.Pending;

    public DateTime? RespondedAt { get; set; }

    // Navigation Properties
    public OpenSessionRequest OpenSessionRequest { get; set; } = null!;
    public Student.Student InvitedStudent { get; set; } = null!;
    public Student.Student InvitedByStudent { get; set; } = null!;
}
