using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// One content unit OR lesson covered in a specific calendar slot of a course enrollment request.
/// Exactly one of <see cref="ContentUnitId"/> or <see cref="LessonId"/> must be set — same invariant
/// as <see cref="Qalam.Data.Entity.OpenSessionRequests.OpenSessionRequestSessionUnit"/>.
/// </summary>
public class CourseRequestSelectedSessionSlotUnit : AuditableEntity
{
    public int Id { get; set; }

    public int CourseRequestSelectedSessionSlotId { get; set; }

    public int? ContentUnitId { get; set; }

    public int? LessonId { get; set; }

    // Navigation Properties
    public CourseRequestSelectedSessionSlot SessionSlot { get; set; } = null!;
    public ContentUnit? ContentUnit { get; set; }
    public Lesson? Lesson { get; set; }
}
