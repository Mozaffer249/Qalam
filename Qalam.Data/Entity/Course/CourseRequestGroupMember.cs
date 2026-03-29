using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// أعضاء المجموعة في طلب التسجيل الجماعي
/// </summary>
public class CourseRequestGroupMember : AuditableEntity
{
    public int Id { get; set; }

    public int CourseEnrollmentRequestId { get; set; }

    /// <summary>
    /// معرف الطالب العضو
    /// </summary>
    public int StudentId { get; set; }

    /// <summary>
    /// نوع العضو: طالب المالك أو مدعو
    /// </summary>
    public GroupMemberType MemberType { get; set; }

    /// <summary>
    /// معرف الطالب الذي دعاه (فقط للأعضاء المدعوين)
    /// </summary>
    public int? InvitedByStudentId { get; set; }

    public GroupMemberConfirmationStatus ConfirmationStatus { get; set; } = GroupMemberConfirmationStatus.Pending;

    public DateTime? ConfirmedAt { get; set; }
    public int? ConfirmedByUserId { get; set; }

    // Navigation Properties
    public CourseEnrollmentRequest CourseEnrollmentRequest { get; set; } = null!;
    public Student.Student Student { get; set; } = null!;
    public Student.Student? InvitedByStudent { get; set; }
}
