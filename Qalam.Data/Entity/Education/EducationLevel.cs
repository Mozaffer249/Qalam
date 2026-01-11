using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class EducationLevel : AuditableEntity
{
    public int Id { get; set; }
    
    public int DomainId { get; set; }
    public EducationDomain Domain { get; set; } = default!;
    
    public int? CurriculumId { get; set; } // nullable للمجالات بدون منهج
    public Curriculum? Curriculum { get; set; }
    
    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;
    
    public int OrderIndex { get; set; }  
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}

