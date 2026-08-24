namespace Qalam.Data.DTOs.Student;

/// <summary>
/// Discover list filters mapped to <see cref="Entity.Teacher.TeacherSubject"/> junction tables.
/// </summary>
public class TeacherSubjectDiscoverFilters
{
    public int? DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public int? ParentSubjectId { get; set; }
    public int? SubjectId { get; set; }
    public List<int>? SubjectIds { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    public int? QuranContentTypeId { get; set; }
    public List<int>? QuranContentTypeIds { get; set; }
    public int? QuranLevelId { get; set; }
    public List<int>? QuranLevelIds { get; set; }
    public List<int>? WritableFilterValueIds { get; set; }
    public List<FieldLevelPairFilter>? FieldLevelPairs { get; set; }
}

/// <summary>Finance domain: writable field × education level pair saved on TeacherSubject.</summary>
public class FieldLevelPairFilter
{
    public int WritableFilterValueId { get; set; }
    public int EducationLevelId { get; set; }
}
