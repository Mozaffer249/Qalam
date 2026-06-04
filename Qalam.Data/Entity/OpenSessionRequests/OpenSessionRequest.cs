using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// طلب جلسات مفتوح من طالب — السيناريو الثاني.
/// الطالب يحدد المادة وعدد الجلسات والمواعيد المفضلة، ثم يستقبل عروضاً من معلمين مؤهلين.
/// </summary>
public class OpenSessionRequest : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// الطالب الذي ستُقدَّم الجلسات له (المتعلِّم). مطلوب — قد يكون نفس RequestedByUserId
    /// عندما يقدم الطالب الطلب بنفسه، أو مختلفاً عندما يقدم وليه الطلب نيابةً عنه.
    /// </summary>
    public int StudentId { get; set; }
 
    /// <summary>
    /// المستخدم (الفاعل) الذي أنشأ الطلب فعلياً. يساوي Student.UserId إذا أنشأ الطالب الطلب بنفسه،
    /// أو Guardian.UserId إذا أنشأ الولي الطلب نيابةً عن الطالب القاصر.
    /// تُستخدم لفحوصات الصلاحيات (من يمكنه تعديل/إلغاء الطلب).
    /// </summary>
    public int RequestedByUserId { get; set; }

    /// <summary>
    /// معرّف الولي الذي أنشأ الطلب (اختياري). يكون فارغاً عندما يُنشئ الطالب طلبه بنفسه.
    /// </summary>
    public int? CreatedByGuardianId { get; set; }

    /// <summary>
    /// المجال التعليمي.
    /// </summary>
    public int DomainId { get; set; }

    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    public int? TermId { get; set; }

    /// <summary>
    /// المادة المطلوبة.
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// طريقة التدريس (فردي/جماعي - FK إلى TeachingMode).
    /// </summary>
    public int TeachingModeId { get; set; }

    /// <summary>
    /// Optional — when set, the request is delivered ONLY to this teacher (single target, no broadcast matching).
    /// The teacher must offer the requested <see cref="SubjectId"/> via an active <c>TeacherSubject</c> row,
    /// and every per-session <c>Units[]</c> entry is hard-validated against that teacher's <c>TeacherSubjectUnits</c>.
    /// When null, the existing broadcast matching algorithm runs at publish time.
    /// </summary>
    public int? TargetedTeacherId { get; set; }

    /// <summary>
    /// نوع المجموعة عند اختيار جماعي: مفتوحة لطلاب إضافيين أو دعوة فقط.
    /// </summary>
    public OfferGroupType? GroupType { get; set; }

    /// <summary>
    /// إجمالي عدد الجلسات المطلوبة. يجب أن يطابق عدد صفوف OpenSessionRequestSession قبل النشر.
    /// </summary>
    public int TotalSessionsCount { get; set; }

    public OpenSessionRequestStatus Status { get; set; } = OpenSessionRequestStatus.Draft;

    public DateTime? PublishedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [MaxLength(1000)]
    public string? StudentNotes { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    // Navigation Properties
    public Student.Student Student { get; set; } = null!;
    public Identity.User RequestedByUser { get; set; } = null!;
    public Student.Guardian? CreatedByGuardian { get; set; }
    public EducationDomain Domain { get; set; } = null!;
    public Curriculum? Curriculum { get; set; }
    public EducationLevel? Level { get; set; }
    public Grade? Grade { get; set; }
    public AcademicTerm? Term { get; set; }
    public Subject Subject { get; set; } = null!;
    public TeachingMode TeachingMode { get; set; } = null!;
    public Teacher.Teacher? TargetedTeacher { get; set; }

    public ICollection<OpenSessionRequestSession> Sessions { get; set; } = new List<OpenSessionRequestSession>();
    public ICollection<OpenSessionRequestAttachment> Attachments { get; set; } = new List<OpenSessionRequestAttachment>();
    public ICollection<OpenSessionRequestInvitation> Invitations { get; set; } = new List<OpenSessionRequestInvitation>();
    public ICollection<OpenSessionRequestTarget> Targets { get; set; } = new List<OpenSessionRequestTarget>();
    public ICollection<OpenSessionOffer> Offers { get; set; } = new List<OpenSessionOffer>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
