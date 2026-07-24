using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// Per-student attendance / rating / note for a <see cref="CourseSchedule"/> session.
/// </summary>
public class SessionAttendance : AuditableEntity
{
    public int Id { get; set; }

    public int CourseScheduleId { get; set; }

    public int StudentId { get; set; }

    public SessionAttendanceStatus Status { get; set; } = SessionAttendanceStatus.Pending;

    /// <summary>Optional teacher→student rating (0–5). Null when not rated.</summary>
    public decimal? Rating { get; set; }

    public string? Note { get; set; }

    public bool IsAutoResolved { get; set; }

    /// <summary>When the student joined (CTA / future stream open).</summary>
    public DateTime? JoinedAt { get; set; }

    public CourseSchedule CourseSchedule { get; set; } = null!;
    public Student.Student Student { get; set; } = null!;
}
