namespace Qalam.Data.DTOs.Teacher;

#region Input DTOs

/// <summary>
/// DTO لحفظ مواد المعلم (قائمة كاملة)
/// </summary>
public class SaveTeacherSubjectsDto
{
    public List<TeacherSubjectItemDto> Subjects { get; set; } = new();
}

/// <summary>
/// DTO لمادة واحدة مع وحداتها
/// </summary>
public class TeacherSubjectItemDto
{
    public int SubjectId { get; set; }

    /// <summary>
    /// هل يمكنه تدريس المادة كاملة؟
    /// true = المادة كاملة، false = وحدات محددة فقط
    /// </summary>
    public bool CanTeachFullSubject { get; set; } = true;

    /// <summary>
    /// الوحدات المحددة (مطلوبة إذا CanTeachFullSubject = false)
    /// </summary>
    public List<TeacherSubjectUnitItemDto> Units { get; set; } = new();

    /// <summary>
    /// Quran content types covered. Empty = all types.
    /// </summary>
    public List<int> QuranContentTypeIds { get; set; } = new();

    /// <summary>
    /// Quran levels covered. Empty = all levels.
    /// </summary>
    public List<int> QuranLevelIds { get; set; } = new();

    /// <summary>
    /// Quran audience bands (EducationLevel). Empty = all audiences.
    /// </summary>
    public List<int> EducationLevelIds { get; set; } = new();

    public List<int> WritableFilterValueIds { get; set; } = new();
}

/// <summary>
/// DTO لوحدة واحدة
/// </summary>
public class TeacherSubjectUnitItemDto
{
    public int UnitId { get; set; }
}

/// <summary>
/// DTO لتحديث وحدات مادة معلم واحدة
/// </summary>
public class UpdateTeacherSubjectDto
{
    public bool CanTeachFullSubject { get; set; }

    public List<TeacherSubjectUnitItemDto> Units { get; set; } = new();

    public List<int> QuranContentTypeIds { get; set; } = new();

    public List<int> QuranLevelIds { get; set; } = new();

    public List<int> EducationLevelIds { get; set; } = new();

    public List<int> WritableFilterValueIds { get; set; } = new();
}

#endregion

#region Response DTOs

/// <summary>
/// DTO لاستجابة مواد المعلم
/// </summary>
public class TeacherSubjectsResponseDto
{
    public int TeacherId { get; set; }
    public List<TeacherSubjectResponseDto> Subjects { get; set; } = new();
    public RegistrationStepDto? NextStep { get; set; }
}

/// <summary>
/// DTO لاستجابة مادة واحدة
/// </summary>
public class TeacherSubjectResponseDto
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string SubjectNameAr { get; set; } = default!;
    public string SubjectNameEn { get; set; } = default!;
    public string? DomainCode { get; set; }
    public string? DomainNameAr { get; set; }
    public string? DomainNameEn { get; set; }

    public int? CurriculumId { get; set; }
    public string? CurriculumNameAr { get; set; }
    public string? CurriculumNameEn { get; set; }

    public int? LevelId { get; set; }
    public string? LevelNameAr { get; set; }
    public string? LevelNameEn { get; set; }

    public int? GradeId { get; set; }
    public string? GradeNameAr { get; set; }
    public string? GradeNameEn { get; set; }

    public bool CanTeachFullSubject { get; set; }
    public bool IsActive { get; set; }

    public List<int> QuranContentTypeIds { get; set; } = new();
    public List<int> QuranLevelIds { get; set; } = new();
    public List<int> EducationLevelIds { get; set; } = new();
    public List<int> WritableFilterValueIds { get; set; } = new();

    public List<TeacherSubjectUnitResponseDto> Units { get; set; } = new();
}

/// <summary>
/// DTO لاستجابة وحدة واحدة
/// </summary>
public class TeacherSubjectUnitResponseDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string UnitNameAr { get; set; } = default!;
    public string UnitNameEn { get; set; } = default!;
    public string? UnitTypeCode { get; set; }
}

/// <summary>
/// Slim unit option for teacher course-create picker (scoped to teacher subject repertoire).
/// </summary>
public class TeacherSubjectUnitOptionDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
}

/// <summary>
/// Full-catalog unit option for the profile edit drawer (with selection state).
/// </summary>
public class TeacherSubjectUnitPickerDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public bool IsSelected { get; set; }
    public int? QuranContentTypeId { get; set; }
    public int? QuranLevelId { get; set; }
}

#endregion
