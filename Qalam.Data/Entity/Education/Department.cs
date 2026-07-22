using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class Department : AuditableEntity
{
    public int Id { get; set; }

    public int CollegeId { get; set; }
    public College College { get; set; } = default!;

    [Required, MaxLength(150)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(150)]
    public string NameEn { get; set; } = default!;

    [MaxLength(50)]
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<AcademicProgram> AcademicPrograms { get; set; } = new List<AcademicProgram>();
}
