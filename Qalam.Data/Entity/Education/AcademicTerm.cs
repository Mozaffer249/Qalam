using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class AcademicTerm : AuditableEntity
{
    public int Id { get; set; }
    
    public int CurriculumId { get; set; }
    public Curriculum Curriculum { get; set; } = default!;
    
    [Required, MaxLength(50)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(50)]
    public string NameEn { get; set; } = default!;
    
    public int OrderIndex { get; set; }
    
    public bool IsMandatory { get; set; } = true; // إلزامي؟
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}

