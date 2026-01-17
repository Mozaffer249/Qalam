using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Student;

/// <summary>
/// كيان الطالب - مرتبط بحساب المستخدم
/// </summary>
public class Student : AuditableEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// معرف المستخدم المرتبط
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// هل الطالب قاصر ويحتاج ولي أمر؟
    /// </summary>
    public bool IsMinor { get; set; } = false;
    
    // المعلومات التعليمية
    public int? DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    
    // المعلومات الشخصية
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    
    [MaxLength(200)]
    public string? SchoolName { get; set; }
    
    [MaxLength(500)]
    public string? Bio { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public User User { get; set; } = null!;
    public EducationDomain? Domain { get; set; }
    public Curriculum? Curriculum { get; set; }
    public EducationLevel? Level { get; set; }
    public Grade? Grade { get; set; }
    
    /// <summary>
    /// أولياء أمور الطالب (Many-to-Many)
    /// </summary>
    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
}
