using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Student;

/// <summary>
/// Student entity related to the user
/// </summary>
public class Student : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// User ID related to the student (required)
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Whether the student is a minor and needs a guardian
    /// </summary>
    public bool IsMinor { get; set; } = false;

    /// <summary>
    /// Guardian ID (optional - required only if the student is a minor)
    /// </summary>
    public int? GuardianId { get; set; }

    /// <summary>
    /// Guardian relation with the student (father, mother, brother, sister, etc.)
    /// </summary>
    public GuardianRelation? GuardianRelation { get; set; }

    // Educational information
    public int? DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }

    // Personal information
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public User User { get; set; } = null!;
    public EducationDomain? Domain { get; set; }
    public Curriculum? Curriculum { get; set; }
    public EducationLevel? Level { get; set; }
    public Grade? Grade { get; set; }

    /// <summary>
    /// Guardian of the student (One-to-Many)
    /// </summary>
    public Guardian? Guardian { get; set; }
}
