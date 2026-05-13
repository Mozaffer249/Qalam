using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

/// <summary>
/// One row in the teacher's "enrollments for this course" list.
/// Backed by unified <c>Enrollment</c> table; <see cref="Kind"/> tells individual / group apart.
/// </summary>
public class TeacherEnrollmentListItemDto
{
    public int Id { get; set; }

    public EnrollmentKind Kind { get; set; }

    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;

    /// <summary>Student name (Individual) or "Group of N — Leader: …" (Group).</summary>
    public string DisplayName { get; set; } = default!;

    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime ApprovedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }

    /// <summary>1 for Individual; total participants for Group.</summary>
    public int ParticipantCount { get; set; }

    /// <summary>Number of participants whose payment Succeeded.</summary>
    public int PaidParticipantCount { get; set; }

    /// <summary>Total CourseSchedule rows attached. 0 until enrollment is Active.</summary>
    public int SessionsCount { get; set; }
}

/// <summary>One participant inside a teacher's enrollment detail view.</summary>
public class TeacherEnrollmentParticipantDto
{
    public int ParticipantId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public bool IsMinor { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal Share { get; set; }
}

/// <summary>
/// Teacher view of an enrollment (individual or group): course + leader (group) + participants + sessions.
/// </summary>
public class TeacherEnrollmentDetailDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public string? TeachingModeNameEn { get; set; }
    public string? SessionTypeNameEn { get; set; }

    public EnrollmentKind Kind { get; set; }
    public int? LeaderStudentId { get; set; }
    public string? LeaderStudentName { get; set; }

    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime ApprovedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRemaining { get; set; }
    public string Currency { get; set; } = "SAR";

    public List<TeacherEnrollmentParticipantDto> Participants { get; set; } = new();

    /// <summary>Generated CourseSchedule rows in chronological order (empty until enrollment Active).</summary>
    public List<EnrollmentSessionItemDto> Sessions { get; set; } = new();
}
