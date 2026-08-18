using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Finance (and similar) field × education-level coverage. Empty = not specified.
/// </summary>
public class TeacherSubjectFieldLevel : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherSubjectId { get; set; }
    public TeacherSubject TeacherSubject { get; set; } = null!;

    public int WritableFilterValueId { get; set; }
    public WritableFilterValue WritableFilterValue { get; set; } = null!;

    public int EducationLevelId { get; set; }
    public EducationLevel EducationLevel { get; set; } = null!;
}
