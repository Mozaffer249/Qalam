using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// Unit/lesson coverage for a single CourseSession. Bridge between CourseSession and the
/// educational content tree. Either ContentUnitId (whole unit) or LessonId (specific lesson)
/// must be set — never both, never neither (enforced by validator, not the DB).
/// Mirrors the OpenSessionRequestSessionUnit shape so the two flows stay aligned.
/// </summary>
public class CourseSessionUnit : AuditableEntity
{
    public int Id { get; set; }

    public int CourseSessionId { get; set; }

    public int? ContentUnitId { get; set; }

    public int? LessonId { get; set; }

    public CourseSession CourseSession { get; set; } = null!;
    public ContentUnit? ContentUnit { get; set; }
    public Lesson? Lesson { get; set; }
}
