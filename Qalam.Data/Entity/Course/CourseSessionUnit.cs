using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// Unit/lesson coverage for a single CourseSession. Bridge between CourseSession and the
/// educational content tree. Exactly one of ContentUnitId, LessonId, or a non-empty
/// CustomUnitLabel must be set — never both, never neither (enforced by validator, not the DB).
/// Mirrors the OpenSessionRequestSessionUnit shape so the two flows stay aligned.
/// </summary>
public class CourseSessionUnit : AuditableEntity
{
    public int Id { get; set; }

    public int CourseSessionId { get; set; }

    public int? ContentUnitId { get; set; }

    public int? LessonId { get; set; }

    /// <summary>
    /// Free-text "Other" unit label when the teacher does not pick a catalog unit/lesson.
    /// Mutually exclusive with ContentUnitId and LessonId.
    /// </summary>
    public string? CustomUnitLabel { get; set; }

    public CourseSession CourseSession { get; set; } = null!;
    public ContentUnit? ContentUnit { get; set; }
    public Lesson? Lesson { get; set; }
}
