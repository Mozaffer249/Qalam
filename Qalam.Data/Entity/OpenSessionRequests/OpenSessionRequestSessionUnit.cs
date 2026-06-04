using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// وحدة/درس مغطاة في جلسة ضمن طلب جلسات. علاقة many-to-many جسر بين OpenSessionRequestSession والمحتوى التعليمي.
/// يمكن ربط ContentUnit أو Lesson (واحد منهما يجب أن يكون موجوداً).
///
/// When <see cref="ContentUnitId"/> is set, the <see cref="IncludesAllLessons"/> flag disambiguates the intent:
///  - <c>true</c>  → the row means "every lesson inside this unit is covered."
///  - <c>false</c> → the row means "this unit as a topic header — no specific lessons committed."
/// The flag MUST be <c>false</c> when <see cref="LessonId"/> is set (single-lesson rows don't expand).
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

    /// <summary>
    /// When <see cref="ContentUnitId"/> is set: <c>true</c> means "this row covers every lesson inside the unit";
    /// <c>false</c> means "this unit as a topic header, no specific lessons committed."
    /// MUST be <c>false</c> when <see cref="LessonId"/> is set — single-lesson rows can't expand.
    /// </summary>
    public bool IncludesAllLessons { get; set; }

    // Navigation Properties
    public OpenSessionRequestSession OpenSessionRequestSession { get; set; } = null!;
    public ContentUnit? ContentUnit { get; set; }
    public Lesson? Lesson { get; set; }
}
