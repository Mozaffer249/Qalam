using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
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

    /// <summary>Optional teacher hourly rate in base currency (SAR). Null = platform DomainSessionPrice.</summary>
    public decimal? CustomPricePerHour { get; set; }

    /// <summary>
    /// When true and <see cref="CustomPricePerHour"/> is set, the student is charged the teacher rate.
    /// When false, the student pays the platform rate; teacher earnings still use the custom rate as base.
    /// </summary>
    public bool ReflectCustomPriceToStudent { get; set; }

    /// <summary>True after first completed session in this domain (or admin unlock).</summary>
    public bool HasCompletedInterviewSession { get; set; }

    public Teacher Teacher { get; set; } = null!;
    public EducationDomain Domain { get; set; } = null!;
    public TeacherLevel? TeacherLevel { get; set; }
}
