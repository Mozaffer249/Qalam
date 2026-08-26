using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

namespace Qalam.Data.Entity.Student;

/// <summary>
/// Audit ledger for lifetime free individual trial (course enroll or OSR).
/// </summary>
public class StudentFreeTrialConsumption : AuditableEntity
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public FreeTrialConsumptionSource Source { get; set; }

    public int EnrollmentId { get; set; }

    public int? OpenSessionRequestId { get; set; }

    public int TeacherId { get; set; }

    public int DomainId { get; set; }

    public int? CourseScheduleId { get; set; }

    public FreeTrialConsumptionStatus Status { get; set; } = FreeTrialConsumptionStatus.Reserved;

    public DateTime ReservedAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public bool RestoredEligibility { get; set; }

    [MaxLength(500)]
    public string? CancelReason { get; set; }

    public int? CancelledByUserId { get; set; }

    public Student Student { get; set; } = null!;
    public Enrollment Enrollment { get; set; } = null!;
    public Course.CourseSchedule? CourseSchedule { get; set; }
}
