using Qalam.Data.Commons;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// One concrete calendar session chosen at Individual enrollment (no CourseEnrollmentRequest).
/// </summary>
public class EnrollmentSelectedSessionSlot : AuditableEntity
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }

    public int SessionNumber { get; set; }

    public int TeacherAvailabilityId { get; set; }

    public DateOnly SessionDate { get; set; }

    public Enrollment Enrollment { get; set; } = null!;
    public TeacherAvailability TeacherAvailability { get; set; } = null!;
    public ICollection<EnrollmentSelectedSessionSlotUnit> Units { get; set; } = new List<EnrollmentSelectedSessionSlotUnit>();
}
