using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class AcademicProgram : AuditableEntity
{
    public int Id { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    [Required, MaxLength(150)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(150)]
    public string NameEn { get; set; } = default!;

    [MaxLength(50)]
    public string? Code { get; set; }

    /// <summary>e.g. Bachelor, Master, PhD, MBBS</summary>
    [MaxLength(50)]
    public string? DegreeType { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<EducationLevel> EducationLevels { get; set; } = new List<EducationLevel>();
    public ICollection<AcademicTerm> AcademicTerms { get; set; } = new List<AcademicTerm>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
