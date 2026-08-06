using Qalam.Data.Commons;
using Qalam.Data.Entity.Quran;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Quran content types this teacher covers for a subject.
/// Empty collection on the parent TeacherSubject means all types.
/// </summary>
public class TeacherSubjectQuranContentType : AuditableEntity
{
    public int Id { get; set; }
    public int TeacherSubjectId { get; set; }
    public int QuranContentTypeId { get; set; }

    public TeacherSubject TeacherSubject { get; set; } = null!;
    public QuranContentType QuranContentType { get; set; } = null!;
}
