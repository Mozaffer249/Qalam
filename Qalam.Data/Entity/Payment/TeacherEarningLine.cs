using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// Accrued teacher earnings (ledger). Created on session completion from pricing snapshot share.
/// </summary>
public class TeacherEarningLine : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int EnrollmentId { get; set; }

    public int? CourseScheduleId { get; set; }

    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";

    public TeacherEarningSource Source { get; set; } = TeacherEarningSource.SessionCompleted;

    public TeacherEarningLineStatus Status { get; set; } = TeacherEarningLineStatus.Pending;

    public int? PayoutItemId { get; set; }

    public Teacher.Teacher Teacher { get; set; } = null!;
    public Course.Enrollment Enrollment { get; set; } = null!;
    public Course.CourseSchedule? CourseSchedule { get; set; }
    public PayoutItem? PayoutItem { get; set; }
}
