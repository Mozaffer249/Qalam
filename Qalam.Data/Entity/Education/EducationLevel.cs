using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class EducationLevel : AuditableEntity
{
    public int Id { get; set; }

    public int DomainId { get; set; }
    public EducationDomain Domain { get; set; } = default!;

    public int? CurriculumId { get; set; }
    public Curriculum? Curriculum { get; set; }

    /// <summary>University path: level scoped to an academic program.</summary>
    public int? AcademicProgramId { get; set; }
    public AcademicProgram? AcademicProgram { get; set; }

    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
