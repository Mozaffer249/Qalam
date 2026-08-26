using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// One-way Excel rule patches for canonical domain codes (Infrastructure-only; mirrors EducationRuleDefaults).
/// </summary>
internal static class CanonicalEducationRuleHelper
{
    private static readonly string[] CanonicalExcelCodes =
    [
        EducationDomainCodes.SoftSkills,
        ..EducationDomainCodes.Wave1SplitFromSkills,
        EducationDomainCodes.Sharia,
    ];

    public static bool IsCanonicalExcelCode(string? code) =>
        !string.IsNullOrEmpty(code) &&
        CanonicalExcelCodes.Contains(code, StringComparer.OrdinalIgnoreCase);

    public static EducationRule CreateRuleForCode(string code, int domainId, DateTime now)
    {
        var rule = new EducationRule
        {
            DomainId = domainId,
            RulesConfigured = true,
            MinSessions = 1,
            MaxSessions = 100,
            DefaultSessionDurationMinutes = 60,
            MinGroupSize = 1,
            MaxGroupSize = 20,
            AllowExtension = true,
            AllowFlexibleCourses = true,
            HasContentUnits = true,
            HasLessons = true,
            CreatedAt = now,
        };

        switch (code.ToLowerInvariant())
        {
            case EducationDomainCodes.SoftSkills:
                rule.HasParentSubject = true;
                rule.HasWritableFilters = true;
                break;
            case EducationDomainCodes.LifeSkills:
            case EducationDomainCodes.Hobbies:
                rule.HasParentSubject = true;
                rule.HasEducationLevel = true;
                rule.EducationLevelAfterSubject = true;
                rule.HasWritableFilters = true;
                break;
            case EducationDomainCodes.TechSkills:
            case EducationDomainCodes.Finance:
            case EducationDomainCodes.Knowledge:
                rule.HasEducationLevel = true;
                rule.EducationLevelAfterSubject = true;
                rule.HasWritableFilters = true;
                break;
            case EducationDomainCodes.Sharia:
                rule.HasParentSubject = true;
                rule.HasEducationLevel = true;
                rule.EducationLevelAfterSubject = true;
                rule.HasWritableFilters = true;
                break;
        }

        return rule;
    }

    public static bool ApplyOneWayPatch(EducationRule rule, string code)
    {
        var dirty = false;

        if (!rule.RulesConfigured) { rule.RulesConfigured = true; dirty = true; }
        if (!rule.HasContentUnits) { rule.HasContentUnits = true; dirty = true; }
        if (!rule.HasLessons) { rule.HasLessons = true; dirty = true; }

        switch (code.ToLowerInvariant())
        {
            case EducationDomainCodes.SoftSkills:
                if (!rule.HasParentSubject) { rule.HasParentSubject = true; dirty = true; }
                if (!rule.HasWritableFilters) { rule.HasWritableFilters = true; dirty = true; }
                break;
            case EducationDomainCodes.LifeSkills:
            case EducationDomainCodes.Hobbies:
                if (!rule.HasParentSubject) { rule.HasParentSubject = true; dirty = true; }
                if (!rule.HasEducationLevel) { rule.HasEducationLevel = true; dirty = true; }
                if (!rule.EducationLevelAfterSubject) { rule.EducationLevelAfterSubject = true; dirty = true; }
                if (!rule.HasWritableFilters) { rule.HasWritableFilters = true; dirty = true; }
                break;
            case EducationDomainCodes.TechSkills:
            case EducationDomainCodes.Finance:
            case EducationDomainCodes.Knowledge:
                if (!rule.HasEducationLevel) { rule.HasEducationLevel = true; dirty = true; }
                if (!rule.EducationLevelAfterSubject) { rule.EducationLevelAfterSubject = true; dirty = true; }
                if (!rule.HasWritableFilters) { rule.HasWritableFilters = true; dirty = true; }
                break;
            case EducationDomainCodes.Sharia:
                if (!rule.HasParentSubject) { rule.HasParentSubject = true; dirty = true; }
                if (!rule.HasEducationLevel) { rule.HasEducationLevel = true; dirty = true; }
                if (!rule.EducationLevelAfterSubject) { rule.EducationLevelAfterSubject = true; dirty = true; }
                if (!rule.HasWritableFilters) { rule.HasWritableFilters = true; dirty = true; }
                break;
        }

        if (dirty)
            rule.UpdatedAt = DateTime.UtcNow;
        return dirty;
    }
}
