using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// الوحدات المحددة التي يدرسها المعلم (إذا لم يكن يدرس المادة كاملة).
/// Quran type/level coverage lives on TeacherSubjectQuranContentType / TeacherSubjectQuranLevel.
/// </summary>
public class TeacherSubjectUnit : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherSubjectId { get; set; }
    public int UnitId { get; set; }

    public TeacherSubject TeacherSubject { get; set; } = null!;
    public ContentUnit Unit { get; set; } = null!;
}
