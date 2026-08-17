using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Quran audience bands (Excel: الفئة / المستوى — الصغار، الكبار، …).
/// Empty collection on the parent TeacherSubject means all audiences.
/// </summary>
public class TeacherSubjectEducationLevel : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherSubjectId { get; set; }
    public TeacherSubject TeacherSubject { get; set; } = null!;

    public int EducationLevelId { get; set; }
    public EducationLevel EducationLevel { get; set; } = null!;
}
