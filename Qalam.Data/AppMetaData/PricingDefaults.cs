using Qalam.Data.Entity.Teacher;

namespace Qalam.Data.AppMetaData;

/// <summary>
/// Default teacher level tiers and per-domain hourly rates for dev/local seeding.
/// </summary>
public static class PricingDefaults
{
    public const string SessionTypeIndividual = "individual";
    public const string SessionTypeGroup = "group";

    public static IReadOnlyList<TeacherLevel> CreateTeacherLevels(DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        return
        [
            new TeacherLevel
            {
                Code = "starter",
                NameAr = "مبتدئ",
                NameEn = "Starter",
                OrderIndex = 1,
                TeacherSharePct = 60m,
                IsActive = true,
                CreatedAt = now
            },
            new TeacherLevel
            {
                Code = "intermediate",
                NameAr = "متوسط",
                NameEn = "Intermediate",
                OrderIndex = 2,
                TeacherSharePct = 70m,
                IsActive = true,
                CreatedAt = now
            },
            new TeacherLevel
            {
                Code = "advanced",
                NameAr = "متقدم",
                NameEn = "Advanced",
                OrderIndex = 3,
                TeacherSharePct = 80m,
                IsActive = true,
                CreatedAt = now
            }
        ];
    }

    /// <summary>Returns individual and group SAR/hour for a domain code. Unknown codes use fallback.</summary>
    public static (decimal Individual, decimal Group) GetDomainRates(string domainCode)
    {
        return domainCode switch
        {
            EducationDomainCodes.School => (100m, 75m),
            EducationDomainCodes.Quran => (80m, 60m),
            EducationDomainCodes.Language => (120m, 90m),
            EducationDomainCodes.University => (150m, 120m),
            EducationDomainCodes.Sharia => (100m, 75m),
            EducationDomainCodes.SoftSkills or EducationDomainCodes.LifeSkills or EducationDomainCodes.TechSkills
                or EducationDomainCodes.Hobbies or EducationDomainCodes.Finance or EducationDomainCodes.Knowledge
                => (90m, 70m),
            _ => (100m, 75m)
        };
    }
}
