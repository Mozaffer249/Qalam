using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// محادثة بين معلم التسجيل والطالب (القائد / المالك). مفتاح فريد على EnrollmentId.
/// </summary>
public class EnrollmentConversation : AuditableEntity
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }

    /// <summary>المعلم المشارك (ApprovedByTeacher / صاحب الدورة).</summary>
    public int TeacherId { get; set; }

    /// <summary>مستخدم الطالب المقابل في الشات (OwnerUser أو Leader أو أول مشارك).</summary>
    public int StudentUserId { get; set; }

    public DateTime? StudentLastReadAt { get; set; }

    public DateTime? TeacherLastReadAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public Enrollment Enrollment { get; set; } = null!;
    public Teacher.Teacher Teacher { get; set; } = null!;
    public Identity.User StudentUser { get; set; } = null!;
    public ICollection<EnrollmentConversationMessage> Messages { get; set; } = new List<EnrollmentConversationMessage>();
}
