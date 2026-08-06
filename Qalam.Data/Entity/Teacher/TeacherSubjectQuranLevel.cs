using Qalam.Data.Commons;
using Qalam.Data.Entity.Quran;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Quran levels this teacher covers for a subject.
/// Empty collection on the parent TeacherSubject means all levels.
/// </summary>
public class TeacherSubjectQuranLevel : AuditableEntity
{
    public int Id { get; set; }
    public int TeacherSubjectId { get; set; }
    public int QuranLevelId { get; set; }

    public TeacherSubject TeacherSubject { get; set; } = null!;
    public QuranLevel QuranLevel { get; set; } = null!;
}
