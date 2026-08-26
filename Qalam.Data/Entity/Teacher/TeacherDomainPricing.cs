using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Per-domain commission level and optional price overrides for a teacher.
/// </summary>
public class TeacherDomainPricing : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int DomainId { get; set; }

    /// <summary>Null until the domain interview/first session unlocks the min tier (or admin assigns).</summary>
    public int? TeacherLevelId { get; set; }

    /// <summary>Per-domain share override (0–100). Null = use TeacherLevel.TeacherSharePct.</summary>
    public decimal? CustomTeacherSharePct { get; set; }

    /// <summary>Optional teacher individual-session hourly rate in base currency (SAR). Null = platform rate.</summary>
    public decimal? CustomIndividualPricePerHour { get; set; }

    /// <summary>Optional teacher group-session hourly rate in base currency (SAR). Null = platform rate.</summary>
    public decimal? CustomGroupPricePerHour { get; set; }

    /// <summary>
    /// When true and <see cref="CustomIndividualPricePerHour"/> is set, the student is charged the teacher individual rate.
    /// </summary>
    public bool ReflectCustomIndividualPriceToStudent { get; set; }

    /// <summary>
    /// When true and <see cref="CustomGroupPricePerHour"/> is set, the student is charged the teacher group rate.
    /// </summary>
    public bool ReflectCustomGroupPriceToStudent { get; set; }

    /// <summary>True after first completed session in this domain (or admin unlock).</summary>
    public bool HasCompletedInterviewSession { get; set; }

    public InterviewUnlockSource InterviewUnlockSource { get; set; } = InterviewUnlockSource.None;

    public int? InterviewUnlockEnrollmentId { get; set; }

    public int? InterviewUnlockCourseScheduleId { get; set; }

    public DateTime? InterviewUnlockedAt { get; set; }

    public DateTime? InterviewRevertedAt { get; set; }

    public Teacher Teacher { get; set; } = null!;
    public EducationDomain Domain { get; set; } = null!;
    public TeacherLevel? TeacherLevel { get; set; }
}
