using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class AcademicTerm : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>School path: term under a curriculum. Nullable for university terms.</summary>
    public int? CurriculumId { get; set; }
    public Curriculum? Curriculum { get; set; }

    /// <summary>University path: semester under an academic program.</summary>
    public int? AcademicProgramId { get; set; }
    public AcademicProgram? AcademicProgram { get; set; }

    [Required, MaxLength(50)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(50)]
    public string NameEn { get; set; } = default!;

    public int OrderIndex { get; set; }

    public bool IsMandatory { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
