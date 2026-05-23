using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// تسجيل (فردي أو جماعي) — موحد لكلا السيناريو الأول (طلب دورة) والسيناريو الثاني (طلب جلسات).
/// </summary>
public class Enrollment : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// مصدر التسجيل. CourseRequest = من طلب دورة (السيناريو الأول)، OpenSessionRequest = من طلب جلسات (السيناريو الثاني).
    /// </summary>
    public EnrollmentSource Source { get; set; } = EnrollmentSource.CourseRequest;

    /// <summary>
    /// معرف الدورة. مطلوب فقط لمصدر CourseRequest، فارغ لمصدر OpenSessionRequest.
    /// </summary>
    public int? CourseId { get; set; }

    /// <summary>
    /// معرف طلب التسجيل الأصلي للسيناريو الأول. فارغ للسيناريو الثاني.
    /// </summary>
    public int? EnrollmentRequestId { get; set; }

    /// <summary>
    /// معرف طلب الجلسات للسيناريو الثاني. فارغ للسيناريو الأول.
    /// </summary>
    public int? SessionRequestId { get; set; }

    /// <summary>
    /// معرف عرض المعلم المقبول للسيناريو الثاني. فارغ للسيناريو الأول.
    /// </summary>
    public int? SessionOfferId { get; set; }

    /// <summary>
    /// شكل التسجيل: فردي أو جماعي. مخزن (غير محسوب) لتبسيط الاستعلامات.
    /// </summary>
    public EnrollmentKind Kind { get; set; }

    /// <summary>
    /// قائد المجموعة — null للتسجيل الفردي.
    /// </summary>
    public int? LeaderStudentId { get; set; }

    /// <summary>
    /// المعلم الذي وافق على الطلب (يدوياً للمرنة، تلقائياً للثابتة، أو صاحب العرض المقبول للسيناريو الثاني).
    /// </summary>
    public int ApprovedByTeacherId { get; set; }

    public DateTime ApprovedAt { get; set; }

    public DateTime? PaymentDeadline { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.PendingPayment;

    // Navigation Properties
    public Course? Course { get; set; }
    public CourseEnrollmentRequest? EnrollmentRequest { get; set; }
    public OpenSessionRequest? OpenSessionRequest { get; set; }
    public OpenSessionOffer? OpenSessionOffer { get; set; }
    public Teacher.Teacher ApprovedByTeacher { get; set; } = null!;
    public Student.Student? LeaderStudent { get; set; }

    public ICollection<EnrollmentParticipant> Participants { get; set; } = new List<EnrollmentParticipant>();
    public ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
}
