using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class Curriculum : AuditableEntity
{
    public int Id { get; set; }
    
    public int DomainId { get; set; }
    public EducationDomain Domain { get; set; } = default!;
    
    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;
    
    [MaxLength(100)]
    public string? Country { get; set; } 
    
    [MaxLength(500)]
    public string? DescriptionAr { get; set; }
    
    [MaxLength(500)]
    public string? DescriptionEn { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public ICollection<EducationLevel> EducationLevels { get; set; } = new List<EducationLevel>();
    public ICollection<AcademicTerm> AcademicTerms { get; set; } = new List<AcademicTerm>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}

