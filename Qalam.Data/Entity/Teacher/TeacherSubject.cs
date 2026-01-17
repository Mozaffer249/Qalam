using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// المواد التي يدرسها المعلم
/// </summary>
public class TeacherSubject : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherId { get; set; }
    
    public int SubjectId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    
    /// <summary>
    /// هل يمكنه تدريس المادة كاملة؟
    /// </summary>
    public bool CanTeachFullSubject { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public Teacher Teacher { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Curriculum? Curriculum { get; set; }
    public EducationLevel? Level { get; set; }
    public Grade? Grade { get; set; }
    
    public ICollection<TeacherSubjectUnit> TeacherSubjectUnits { get; set; } = new List<TeacherSubjectUnit>();
}
