using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// تسجيل في دورة (فردي أو جماعي) — موحد لكلا الحالتين.
/// </summary>
public class Enrollment : AuditableEntity
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    /// <summary>
    /// معرف طلب التسجيل الأصلي (مطلوب لتوليد الجدول من Availabilities/ProposedSessions).
    /// </summary>
    public int? EnrollmentRequestId { get; set; }

    /// <summary>
    /// شكل التسجيل: فردي أو جماعي. مخزن (غير محسوب) لتبسيط الاستعلامات.
    /// </summary>
    public EnrollmentKind Kind { get; set; }

    /// <summary>
    /// قائد المجموعة — null للتسجيل الفردي.
    /// </summary>
    public int? LeaderStudentId { get; set; }

    /// <summary>
    /// المعلم الذي وافق على الطلب (يدوياً للمرنة، تلقائياً للثابتة).
    /// </summary>
    public int ApprovedByTeacherId { get; set; }

    public DateTime ApprovedAt { get; set; }

    public DateTime? PaymentDeadline { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.PendingPayment;

    // Navigation Properties
    public Course Course { get; set; } = null!;
    public CourseEnrollmentRequest? EnrollmentRequest { get; set; }
    public Teacher.Teacher ApprovedByTeacher { get; set; } = null!;
    public Student.Student? LeaderStudent { get; set; }

    public ICollection<EnrollmentParticipant> Participants { get; set; } = new List<EnrollmentParticipant>();
    public ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
}
