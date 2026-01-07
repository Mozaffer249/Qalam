using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class Grade : AuditableEntity
{
    public int Id { get; set; }
    
    public int LevelId { get; set; }
    public EducationLevel Level { get; set; } = default!;
    
    [Required, MaxLength(50)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(50)]
    public string NameEn { get; set; } = default!;
    
    public int OrderIndex { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}

