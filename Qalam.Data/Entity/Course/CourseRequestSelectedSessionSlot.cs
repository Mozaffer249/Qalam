using Qalam.Data.Commons;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// One concrete calendar session chosen at enrollment (date + weekly teacher slot).
/// </summary>
public class CourseRequestSelectedSessionSlot : AuditableEntity
{
    public int Id { get; set; }

    public int CourseEnrollmentRequestId { get; set; }

    public int SessionNumber { get; set; }

    public int TeacherAvailabilityId { get; set; }

    public DateOnly SessionDate { get; set; }

    public CourseEnrollmentRequest CourseEnrollmentRequest { get; set; } = null!;
    public TeacherAvailability TeacherAvailability { get; set; } = null!;
}
