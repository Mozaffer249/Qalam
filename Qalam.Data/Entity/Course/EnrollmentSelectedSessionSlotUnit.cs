using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// One content unit OR lesson covered in a specific calendar slot of a direct enrollment.
/// Exactly one of <see cref="ContentUnitId"/> or <see cref="LessonId"/> must be set.
/// </summary>
public class EnrollmentSelectedSessionSlotUnit : AuditableEntity
{
    public int Id { get; set; }

    public int EnrollmentSelectedSessionSlotId { get; set; }

    public int? ContentUnitId { get; set; }

    public int? LessonId { get; set; }

    public EnrollmentSelectedSessionSlot SessionSlot { get; set; } = null!;
    public ContentUnit? ContentUnit { get; set; }
    public Lesson? Lesson { get; set; }
}
