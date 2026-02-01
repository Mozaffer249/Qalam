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
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    
    /// <summary>
    /// هل يمكنه تدريس المادة كاملة؟
    /// true = المادة كاملة، false = وحدات محددة فقط
    /// </summary>
    public bool CanTeachFullSubject { get; set; } = true;
    
    /// <summary>
    /// الوحدات المحددة (مطلوبة إذا CanTeachFullSubject = false)
    /// </summary>
    public List<TeacherSubjectUnitItemDto> Units { get; set; } = new();
}

/// <summary>
/// DTO لوحدة واحدة مع تخصصات القرآن
/// </summary>
public class TeacherSubjectUnitItemDto
{
    public int UnitId { get; set; }
    
    /// <summary>
    /// نوع المحتوى للقرآن (1=حفظ، 2=تلاوة، 3=تجويد)
    /// null = كل الأنواع
    /// </summary>
    public int? QuranContentTypeId { get; set; }
    
    /// <summary>
    /// مستوى القرآن (1=نوراني، 2=مبتدئ، 3=متوسط، 4=متقدم)
    /// null = كل المستويات
    /// </summary>
    public int? QuranLevelId { get; set; }
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
    
    public int? QuranContentTypeId { get; set; }
    public string? QuranContentTypeNameAr { get; set; }
    public string? QuranContentTypeNameEn { get; set; }
    
    public int? QuranLevelId { get; set; }
    public string? QuranLevelNameAr { get; set; }
    public string? QuranLevelNameEn { get; set; }
}

#endregion
