using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// وحدة/درس مغطاة في جلسة ضمن طلب جلسات. علاقة many-to-many جسر بين OpenSessionRequestSession والمحتوى التعليمي.
/// يمكن ربط ContentUnit أو Lesson (واحد منهما يجب أن يكون موجوداً).
/// </summary>
public class OpenSessionRequestSessionUnit : AuditableEntity
{
    public int Id { get; set; }

    public int SessionRequestSessionId { get; set; }

    /// <summary>
    /// وحدة محتوى (ContentUnit) - فارغ إذا كانت دروس مفصلة.
    /// </summary>
    public int? ContentUnitId { get; set; }

    /// <summary>
    /// درس مفصل (Lesson) - فارغ إذا كانت وحدة كاملة.
    /// </summary>
    public int? LessonId { get; set; }

    // Navigation Properties
    public OpenSessionRequestSession OpenSessionRequestSession { get; set; } = null!;
    public ContentUnit? ContentUnit { get; set; }
    public Lesson? Lesson { get; set; }
}
