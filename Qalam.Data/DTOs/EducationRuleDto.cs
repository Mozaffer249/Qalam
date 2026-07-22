namespace Qalam.Data.DTOs;

public class EducationRuleDto
{
    public int? Id { get; set; }
    public int? DomainId { get; set; }

    public bool HasCurriculum { get; set; }
    public bool HasEducationLevel { get; set; }
    public bool HasGrade { get; set; }
    public bool HasAcademicTerm { get; set; }
    public bool HasContentUnits { get; set; }
    public bool HasLessons { get; set; }

    public bool HasUniversity { get; set; }
    public bool HasCollege { get; set; }
    public bool HasDepartment { get; set; }
    public bool HasAcademicProgram { get; set; }
    public bool AcademicTermOptional { get; set; }

    public bool RequiresQuranContentType { get; set; }
    public bool RequiresQuranLevel { get; set; }
    public bool RequiresUnitTypeSelection { get; set; }

    public int MinSessions { get; set; } = 1;
    public int MaxSessions { get; set; } = 100;
    public int DefaultSessionDurationMinutes { get; set; } = 60;

    public bool AllowExtension { get; set; } = true;
    public bool AllowFlexibleCourses { get; set; } = false;

    public int? MaxGroupSize { get; set; }
    public int? MinGroupSize { get; set; }

    public string? NotesAr { get; set; }
    public string? NotesEn { get; set; }

    public bool RulesConfigured { get; set; }
}
