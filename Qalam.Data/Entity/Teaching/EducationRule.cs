using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teaching;

public class EducationRule : AuditableEntity
{
    public int Id { get; set; }

    public int DomainId { get; set; }

    [JsonIgnore]
    public EducationDomain Domain { get; set; } = default!;

    // Tree structure rules (UI/validation)
    public bool HasCurriculum { get; set; }
    public bool HasEducationLevel { get; set; }
    public bool HasGrade { get; set; }
    public bool HasAcademicTerm { get; set; }
    public bool HasContentUnits { get; set; }
    public bool HasLessons { get; set; }

    // Quran pedagogy requirements
    public bool RequiresQuranContentType { get; set; }
    public bool RequiresQuranLevel { get; set; }
    public bool RequiresUnitTypeSelection { get; set; }

    // قواعد الجلسات
    public int MinSessions { get; set; } = 1;
    public int MaxSessions { get; set; } = 100;
    public int DefaultSessionDurationMinutes { get; set; } = 60;

    // المرونة
    public bool AllowExtension { get; set; } = true;
    public bool AllowFlexibleCourses { get; set; } = false;

    // الجلسات الجماعية
    public int? MaxGroupSize { get; set; }
    public int? MinGroupSize { get; set; }

    // ملاحظات
    [MaxLength(500)]
    public string? NotesAr { get; set; }

    [MaxLength(500)]
    public string? NotesEn { get; set; }

    /// <summary>
    /// True after an admin explicitly saves rules on the domain rules page.
    /// </summary>
    public bool RulesConfigured { get; set; }
}

