using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

/// <summary>
/// Discriminator for teacher's enrollment list — individual or group.
/// </summary>
public enum TeacherEnrollmentKind
{
    Individual = 0,
    Group = 1
}

/// <summary>
/// One row in the teacher's "enrollments for this course" list. Mixes individual
/// CourseEnrollment and group CourseGroupEnrollment rows; <see cref="Kind"/> tells them apart.
/// </summary>
public class TeacherEnrollmentListItemDto
{
    /// <summary>
    /// CourseEnrollment.Id when Kind = Individual; CourseGroupEnrollment.Id when Kind = Group.
    /// </summary>
    public int Id { get; set; }

    public TeacherEnrollmentKind Kind { get; set; }

    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;

    /// <summary>Student name (Individual) or "Group of N — Leader: …" (Group).</summary>
    public string DisplayName { get; set; } = default!;

    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime ApprovedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }

    /// <summary>1 for Individual; total members for Group.</summary>
    public int MemberCount { get; set; }

    /// <summary>Number of members whose payment Succeeded (1 for paid Individual; 0..N for Group).</summary>
    public int PaidMemberCount { get; set; }

    /// <summary>Total CourseSchedule rows attached. 0 until enrollment is Active.</summary>
    public int SessionsCount { get; set; }
}

/// <summary>Minimal student summary for teacher's enrollment views.</summary>
public class TeacherEnrollmentStudentDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public bool IsMinor { get; set; }
}

/// <summary>
/// Teacher view of an individual course enrollment: course summary + payment status + generated sessions.
/// </summary>
public class TeacherEnrollmentDetailDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public string? TeachingModeNameEn { get; set; }
    public string? SessionTypeNameEn { get; set; }

    public TeacherEnrollmentStudentDto Student { get; set; } = default!;

    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime ApprovedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }

    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string Currency { get; set; } = "SAR";

    /// <summary>Generated CourseSchedule rows in chronological order (empty until paid + Active).</summary>
    public List<EnrollmentSessionItemDto> Sessions { get; set; } = new();
}

/// <summary>One member row inside a teacher's group-enrollment detail view.</summary>
public class TeacherGroupEnrollmentMemberDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public bool IsMinor { get; set; }
    public GroupMemberType MemberType { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal Share { get; set; }
}

/// <summary>
/// Teacher view of a group enrollment: course summary + leader + per-member payment + generated sessions.
/// </summary>
public class TeacherGroupEnrollmentDetailDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = default!;
    public string? TeachingModeNameEn { get; set; }
    public string? SessionTypeNameEn { get; set; }

    public int LeaderStudentId { get; set; }
    public string? LeaderStudentName { get; set; }

    public EnrollmentStatus Status { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRemaining { get; set; }
    public string Currency { get; set; } = "SAR";

    public List<TeacherGroupEnrollmentMemberDto> Members { get; set; } = new();

    /// <summary>Generated CourseSchedule rows in chronological order (empty until ALL members paid + group Active).</summary>
    public List<EnrollmentSessionItemDto> Sessions { get; set; } = new();
}
