using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Language (and similar) CEFR/grade coverage. Empty = not specified.
/// </summary>
public class TeacherSubjectGrade : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherSubjectId { get; set; }
    public TeacherSubject TeacherSubject { get; set; } = null!;

    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
}
