using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Data.Entity.Education;

public class EducationDomain : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;

    [Required, MaxLength(50)]
    public string Code { get; set; } = default!; // school, quran, language, skills

    public bool HasCurriculum { get; set; }

    [MaxLength(500)]
    public string? DescriptionAr { get; set; }

    [MaxLength(500)]
    public string? DescriptionEn { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<EducationLevel> EducationLevels { get; set; } = new List<EducationLevel>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<DomainTeachingMode> DomainTeachingModes { get; set; } = new List<DomainTeachingMode>();
    public EducationRule? EducationRule { get; set; }
}

