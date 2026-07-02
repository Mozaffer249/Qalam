using Qalam.Data.DTOs;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Service;

public static class EducationRuleDefaults
{
    public static EducationRuleDto ForDomainCode(string code) =>
        code.ToLowerInvariant() switch
        {
            "school" => School(),
            "quran" => Quran(),
            "language" => Language(),
            "skills" => Skills(),
            "university" => University(),
            _ => Generic(),
        };

    public static EducationRuleDto Generic() => new()
    {
        HasCurriculum = false,
        HasEducationLevel = false,
        HasGrade = false,
        HasAcademicTerm = false,
        HasContentUnits = true,
        HasLessons = true,
        RequiresQuranContentType = false,
        RequiresQuranLevel = false,
        RequiresUnitTypeSelection = false,
        MinSessions = 1,
        MaxSessions = 100,
        DefaultSessionDurationMinutes = 60,
        MinGroupSize = 1,
        MaxGroupSize = 20,
        AllowExtension = true,
        AllowFlexibleCourses = true,
    };

    public static EducationRuleDto School() => new()
    {
        HasCurriculum = true,
        HasEducationLevel = true,
        HasGrade = true,
        HasAcademicTerm = true,
        HasContentUnits = true,
        HasLessons = true,
        MinSessions = 1,
        MaxSessions = 200,
        DefaultSessionDurationMinutes = 45,
        MinGroupSize = 1,
        MaxGroupSize = 30,
        AllowExtension = true,
        AllowFlexibleCourses = true,
    };

    public static EducationRuleDto Quran() => new()
    {
        HasContentUnits = true,
        RequiresQuranContentType = true,
        RequiresQuranLevel = true,
        RequiresUnitTypeSelection = true,
        MinSessions = 1,
        MaxSessions = 300,
        DefaultSessionDurationMinutes = 60,
        MinGroupSize = 1,
        MaxGroupSize = 10,
        AllowExtension = true,
        AllowFlexibleCourses = true,
    };

    public static EducationRuleDto Language() => new()
    {
        HasEducationLevel = true,
        HasContentUnits = true,
        HasLessons = true,
        MinSessions = 1,
        MaxSessions = 150,
        DefaultSessionDurationMinutes = 60,
        MinGroupSize = 1,
        MaxGroupSize = 15,
        AllowExtension = true,
        AllowFlexibleCourses = true,
    };

    public static EducationRuleDto Skills() => new()
    {
        HasContentUnits = true,
        HasLessons = true,
        MinSessions = 1,
        MaxSessions = 100,
        DefaultSessionDurationMinutes = 60,
        MinGroupSize = 1,
        MaxGroupSize = 20,
        AllowExtension = true,
        AllowFlexibleCourses = true,
    };

    public static EducationRuleDto University() => new()
    {
        HasCurriculum = true,
        HasEducationLevel = true,
        HasAcademicTerm = true,
        HasContentUnits = true,
        HasLessons = true,
        MinSessions = 1,
        MaxSessions = 250,
        DefaultSessionDurationMinutes = 90,
        MinGroupSize = 1,
        MaxGroupSize = 40,
        AllowExtension = true,
        AllowFlexibleCourses = true,
    };

    public static EducationRule MapToEntity(EducationRuleDto dto, int domainId, EducationRule? existing = null)
    {
        var rule = existing ?? new EducationRule { DomainId = domainId };
        rule.HasCurriculum = dto.HasCurriculum;
        rule.HasEducationLevel = dto.HasEducationLevel;
        rule.HasGrade = dto.HasGrade;
        rule.HasAcademicTerm = dto.HasAcademicTerm;
        rule.HasContentUnits = dto.HasContentUnits;
        rule.HasLessons = dto.HasLessons;
        rule.RequiresQuranContentType = dto.RequiresQuranContentType;
        rule.RequiresQuranLevel = dto.RequiresQuranLevel;
        rule.RequiresUnitTypeSelection = dto.RequiresUnitTypeSelection;
        rule.MinSessions = dto.MinSessions;
        rule.MaxSessions = dto.MaxSessions;
        rule.DefaultSessionDurationMinutes = dto.DefaultSessionDurationMinutes;
        rule.AllowExtension = dto.AllowExtension;
        rule.AllowFlexibleCourses = dto.AllowFlexibleCourses;
        rule.MinGroupSize = dto.MinGroupSize;
        rule.MaxGroupSize = dto.MaxGroupSize;
        rule.NotesAr = dto.NotesAr;
        rule.NotesEn = dto.NotesEn;
        return rule;
    }

    public static EducationRuleDto MapToDto(EducationRule rule) => new()
    {
        Id = rule.Id,
        DomainId = rule.DomainId,
        HasCurriculum = rule.HasCurriculum,
        HasEducationLevel = rule.HasEducationLevel,
        HasGrade = rule.HasGrade,
        HasAcademicTerm = rule.HasAcademicTerm,
        HasContentUnits = rule.HasContentUnits,
        HasLessons = rule.HasLessons,
        RequiresQuranContentType = rule.RequiresQuranContentType,
        RequiresQuranLevel = rule.RequiresQuranLevel,
        RequiresUnitTypeSelection = rule.RequiresUnitTypeSelection,
        MinSessions = rule.MinSessions,
        MaxSessions = rule.MaxSessions,
        DefaultSessionDurationMinutes = rule.DefaultSessionDurationMinutes,
        AllowExtension = rule.AllowExtension,
        AllowFlexibleCourses = rule.AllowFlexibleCourses,
        MinGroupSize = rule.MinGroupSize,
        MaxGroupSize = rule.MaxGroupSize,
        NotesAr = rule.NotesAr,
        NotesEn = rule.NotesEn,
    };
}
