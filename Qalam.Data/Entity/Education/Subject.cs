using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class Subject : AuditableEntity
{
    public int Id { get; set; }
    
    public int DomainId { get; set; }
    public EducationDomain Domain { get; set; } = default!;
    
    public int? CurriculumId { get; set; }
    public Curriculum? Curriculum { get; set; }
    
    public int? LevelId { get; set; }
    public EducationLevel? Level { get; set; }
    
    public int? GradeId { get; set; }
    public Grade? Grade { get; set; }
    
    public int? TermId { get; set; }
    public AcademicTerm? Term { get; set; }
    
    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;
    
    [MaxLength(500)]
    public string? DescriptionAr { get; set; }
    
    [MaxLength(500)]
    public string? DescriptionEn { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public ICollection<ContentUnit> ContentUnits { get; set; } = new List<ContentUnit>();
    public ICollection<Teacher.TeacherSubject> TeacherSubjects { get; set; } = new List<Teacher.TeacherSubject>();
}

