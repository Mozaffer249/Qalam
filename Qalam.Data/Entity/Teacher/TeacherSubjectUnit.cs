using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// الوحدات المحددة التي يدرسها المعلم (إذا لم يكن يدرس المادة كاملة)
/// </summary>
public class TeacherSubjectUnit : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherSubjectId { get; set; }
    public int UnitId { get; set; }
    
    // Navigation Properties
    public TeacherSubject TeacherSubject { get; set; } = null!;
    public ContentUnit Unit { get; set; } = null!;
}
