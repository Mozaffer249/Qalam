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

    /// <summary>
    /// هل يمكنه تدريس المادة كاملة؟
    /// </summary>
    public bool CanTeachFullSubject { get; set; } = true;

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public Teacher Teacher { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<TeacherSubjectUnit> TeacherSubjectUnits { get; set; } = new List<TeacherSubjectUnit>();
    public ICollection<TeacherSubjectQuranContentType> QuranContentTypes { get; set; } = new List<TeacherSubjectQuranContentType>();
    public ICollection<TeacherSubjectQuranLevel> QuranLevels { get; set; } = new List<TeacherSubjectQuranLevel>();
    public ICollection<TeacherSubjectEducationLevel> EducationLevels { get; set; } = new List<TeacherSubjectEducationLevel>();
    public ICollection<TeacherSubjectGrade> Grades { get; set; } = new List<TeacherSubjectGrade>();
    public ICollection<TeacherSubjectWritableFilter> WritableFilters { get; set; } = new List<TeacherSubjectWritableFilter>();
    public ICollection<TeacherSubjectFieldLevel> FieldLevels { get; set; } = new List<TeacherSubjectFieldLevel>();
}
