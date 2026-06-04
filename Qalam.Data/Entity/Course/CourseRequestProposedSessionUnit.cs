using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// One content unit OR lesson covered in a proposed (flexible-course) session of a course enrollment request.
/// Exactly one of <see cref="ContentUnitId"/> or <see cref="LessonId"/> must be set.
/// </summary>
public class CourseRequestProposedSessionUnit : AuditableEntity
{
    public int Id { get; set; }

    public int CourseRequestProposedSessionId { get; set; }

    public int? ContentUnitId { get; set; }

    public int? LessonId { get; set; }

    // Navigation Properties
    public CourseRequestProposedSession ProposedSession { get; set; } = null!;
    public ContentUnit? ContentUnit { get; set; }
    public Lesson? Lesson { get; set; }
}
