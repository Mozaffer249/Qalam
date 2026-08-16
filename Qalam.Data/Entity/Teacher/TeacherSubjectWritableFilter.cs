using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

public class TeacherSubjectWritableFilter : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherSubjectId { get; set; }
    public TeacherSubject TeacherSubject { get; set; } = default!;

    public int WritableFilterValueId { get; set; }
    public WritableFilterValue WritableFilterValue { get; set; } = default!;
}
