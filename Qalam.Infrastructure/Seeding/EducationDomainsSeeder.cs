using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class EducationDomainsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        // Safely check if data exists (returns false if table doesn't exist)
        if (!await SeederHelper.HasAnyDataAsync(context.EducationDomains))
        {
            var domains = new List<EducationDomain>
            {
                // School Domain
                new()
                {
                    NameAr = "تعليم مدرسي",
                    NameEn = "School Education",
                    Code = "school",
                    DescriptionAr = "التعليم المدرسي الأكاديمي بجميع مراحله",
                    DescriptionEn = "Academic school education at all levels",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
                    {
                        HasCurriculum = true,
                        HasEducationLevel = true,
                        HasGrade = true,
                        HasAcademicTerm = true,
                        HasContentUnits = true,
                        HasLessons = true,
                        RulesConfigured = true,
                        RequiresQuranContentType = false,
                        RequiresQuranLevel = false,
                        RequiresUnitTypeSelection = false,
                        MinSessions = 1,
                        MaxSessions = 200,
                        DefaultSessionDurationMinutes = 45,
                        MinGroupSize = 1,
                        MaxGroupSize = 30,
                        AllowExtension = true,
                        AllowFlexibleCourses = true,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                // Quran Domain
                new()
                {
                    NameAr = "قرآن كريم",
                    NameEn = "Quran",
                    Code = "quran",
                    DescriptionAr = "تعليم القرآن الكريم حفظاً وتلاوة وتجويداً",
                    DescriptionEn = "Quran education: memorization, recitation, and tajweed",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
                    {
                        HasCurriculum = false,
                        HasEducationLevel = true,
                        HasGrade = false,
                        HasAcademicTerm = false,
                        HasContentUnits = true,
                        HasLessons = false,
                        HasWritableFilters = true,
                        RulesConfigured = true,
                        RequiresQuranContentType = true,
                        RequiresQuranLevel = false,
                        RequiresUnitTypeSelection = true,
                        MinSessions = 1,
                        MaxSessions = 300,
                        DefaultSessionDurationMinutes = 60,
                        MinGroupSize = 1,
                        MaxGroupSize = 10,
                        AllowExtension = true,
                        AllowFlexibleCourses = true,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                // Language Domain
                new()
                {
                    NameAr = "لغات",
                    NameEn = "Languages",
                    Code = "language",
                    DescriptionAr = "تعليم اللغات الأجنبية والعربية",
                    DescriptionEn = "Foreign and Arabic language education",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
                    {
                        HasCurriculum = false,
                        HasEducationLevel = true,
                        HasGrade = true,
                        HasAcademicTerm = false,
                        HasContentUnits = true,
                        HasLessons = true,
                        HasWritableFilters = true,
                        RulesConfigured = true,
                        RequiresQuranContentType = false,
                        RequiresQuranLevel = false,
                        RequiresUnitTypeSelection = false,
                        MinSessions = 1,
                        MaxSessions = 150,
                        DefaultSessionDurationMinutes = 60,
                        MinGroupSize = 1,
                        MaxGroupSize = 15,
                        AllowExtension = true,
                        AllowFlexibleCourses = true,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                // Skills Domain (legacy — deactivated after wave-1 split)
                new()
                {
                    NameAr = "مهارات عامة",
                    NameEn = "General Skills",
                    Code = EducationDomainCodes.Skills,
                    DescriptionAr = "المهارات الحياتية والمهنية والتقنية",
                    DescriptionEn = "Life, professional, and technical skills",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
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
                        CreatedAt = DateTime.UtcNow
                    }
                },
                // University Domain
                new()
                {
                    NameAr = "تعليم جامعي",
                    NameEn = "University Education",
                    Code = "university",
                    DescriptionAr = "التعليم الجامعي والدراسات العليا",
                    DescriptionEn = "University and higher education",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
                    {
                        HasCurriculum = false,
                        HasEducationLevel = true,
                        HasGrade = false,
                        HasAcademicTerm = true,
                        AcademicTermOptional = true,
                        HasContentUnits = true,
                        HasLessons = true,
                        HasUniversity = true,
                        HasCollege = true,
                        HasDepartment = true,
                        HasAcademicProgram = true,
                        RulesConfigured = true,
                        RequiresQuranContentType = false,
                        RequiresQuranLevel = false,
                        RequiresUnitTypeSelection = false,
                        MinSessions = 1,
                        MaxSessions = 250,
                        DefaultSessionDurationMinutes = 90,
                        MinGroupSize = 1,
                        MaxGroupSize = 40,
                        AllowExtension = true,
                        AllowFlexibleCourses = false,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            domains.AddRange(CreateWave1Domains());
            domains.Add(CreateShariaDomain(DateTime.UtcNow));

            await context.EducationDomains.AddRangeAsync(domains);
            await context.SaveChangesAsync();
        }

        await EnsureWave1DomainsAsync(context);
        await EnsureExcelCoreDomainsAsync(context);
        await EnsureShariaDomainAsync(context);

        // Backfill university institutional rule flags on existing DBs
        var universityDomain = await context.EducationDomains
            .Include(d => d.EducationRule)
            .FirstOrDefaultAsync(d => d.Code == "university");
        if (universityDomain?.EducationRule is { } uniRule)
        {
            var dirty = false;
            if (!uniRule.HasUniversity) { uniRule.HasUniversity = true; dirty = true; }
            if (!uniRule.HasCollege) { uniRule.HasCollege = true; dirty = true; }
            if (!uniRule.HasDepartment) { uniRule.HasDepartment = true; dirty = true; }
            if (!uniRule.HasAcademicProgram) { uniRule.HasAcademicProgram = true; dirty = true; }
            if (!uniRule.AcademicTermOptional) { uniRule.AcademicTermOptional = true; dirty = true; }
            if (uniRule.HasCurriculum) { uniRule.HasCurriculum = false; dirty = true; }
            if (uniRule.HasGrade) { uniRule.HasGrade = false; dirty = true; }
            if (!uniRule.HasContentUnits) { uniRule.HasContentUnits = true; dirty = true; }
            if (!uniRule.HasLessons) { uniRule.HasLessons = true; dirty = true; }
            if (!uniRule.RulesConfigured) { uniRule.RulesConfigured = true; dirty = true; }
            if (dirty)
            {
                uniRule.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
    }

    private static List<EducationDomain> CreateWave1Domains()
    {
        var now = DateTime.UtcNow;
        return
        [
            CreateDomain(EducationDomainCodes.SoftSkills, "المهارات العملية والناعمة", "Practical and Soft Skills",
                "مهارات العمل والتواصل والقيادة", "Workplace, communication, and leadership skills",
                HasParentSubject: true, HasLevel: false, LevelAfterSubject: false, now),
            CreateDomain(EducationDomainCodes.LifeSkills, "المهارات الحياتية وتطوير الذات", "Life Skills and Self-Development",
                "تطوير الذات والأسرة والعلاقات", "Self-development, family, and relationships",
                HasParentSubject: true, HasLevel: true, LevelAfterSubject: true, now),
            CreateDomain(EducationDomainCodes.TechSkills, "المهارات التقنية", "Technical Skills",
                "البرمجة والشبكات والتصميم والتقنية", "Programming, networks, design, and technology",
                HasParentSubject: false, HasLevel: true, LevelAfterSubject: true, now),
            CreateDomain(EducationDomainCodes.Hobbies, "المهارات الشخصية والهوايات", "Personal Skills and Hobbies",
                "الهوايات والحرف ومجموعات الاهتمام", "Hobbies, crafts, and interest groups",
                HasParentSubject: true, HasLevel: true, LevelAfterSubject: true, now),
            CreateDomain(EducationDomainCodes.Finance, "المال والاستثمار", "Money and Investment",
                "الاستثمار والادخار والتخطيط المالي", "Investing, saving, and financial planning",
                HasParentSubject: false, HasLevel: true, LevelAfterSubject: true, now),
            CreateDomain(EducationDomainCodes.Knowledge, "العلوم والثقافة والمعرفة", "Science, Culture, and Knowledge",
                "المعرفة العامة والعلوم والثقافة", "General knowledge, science, and culture",
                HasParentSubject: false, HasLevel: true, LevelAfterSubject: true, now)
        ];
    }

    private static EducationDomain CreateDomain(
        string code,
        string nameAr,
        string nameEn,
        string descAr,
        string descEn,
        bool HasParentSubject,
        bool HasLevel,
        bool LevelAfterSubject,
        DateTime now) =>
        new()
        {
            NameAr = nameAr,
            NameEn = nameEn,
            Code = code,
            DescriptionAr = descAr,
            DescriptionEn = descEn,
            IsActive = true,
            CreatedAt = now,
            EducationRule = new EducationRule
            {
                HasParentSubject = HasParentSubject,
                HasEducationLevel = HasLevel,
                EducationLevelAfterSubject = LevelAfterSubject,
                HasWritableFilters = true,
                HasContentUnits = true,
                HasLessons = true,
                RulesConfigured = true,
                MinSessions = 1,
                MaxSessions = 100,
                DefaultSessionDurationMinutes = 60,
                MinGroupSize = 1,
                MaxGroupSize = 20,
                AllowExtension = true,
                AllowFlexibleCourses = true,
                CreatedAt = now
            }
        };

    private static async Task EnsureWave1DomainsAsync(ApplicationDBContext context)
    {
        var existing = await context.EducationDomains
            .Include(d => d.EducationRule)
            .ToListAsync();
        var existingCodes = existing.Select(d => d.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = CreateWave1Domains()
            .Where(d => !existingCodes.Contains(d.Code))
            .ToList();
        if (missing.Count > 0)
        {
            await context.EducationDomains.AddRangeAsync(missing);
            await context.SaveChangesAsync();
        }

        var wave1Dirty = false;
        foreach (var domain in existing.Where(d =>
                     EducationDomainCodes.Wave1SplitFromSkills.Contains(d.Code)))
        {
            var rule = domain.EducationRule;
            if (rule is null)
                continue;

            var dirty = false;
            if (!rule.HasContentUnits) { rule.HasContentUnits = true; dirty = true; }
            if (!rule.HasLessons) { rule.HasLessons = true; dirty = true; }
            if (!rule.RulesConfigured) { rule.RulesConfigured = true; dirty = true; }
            if (dirty)
            {
                rule.UpdatedAt = DateTime.UtcNow;
                wave1Dirty = true;
            }
        }

        if (wave1Dirty)
            await context.SaveChangesAsync();

        var skills = existing.FirstOrDefault(d => d.Code == EducationDomainCodes.Skills);
        if (skills is { IsActive: true })
        {
            skills.IsActive = false;
            skills.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    private static EducationDomain CreateShariaDomain(DateTime now) =>
        new()
        {
            NameAr = "علوم الشريعة واللغة العربية",
            NameEn = "Sharia and Arabic Sciences",
            Code = EducationDomainCodes.Sharia,
            DescriptionAr = "العلوم الشرعية وعلوم اللغة العربية",
            DescriptionEn = "Islamic sciences and Arabic language sciences",
            IsActive = true,
            CreatedAt = now,
            EducationRule = new EducationRule
            {
                HasParentSubject = true,
                HasWritableFilters = true,
                HasEducationLevel = true,
                EducationLevelAfterSubject = true,
                HasContentUnits = true,
                HasLessons = true,
                RulesConfigured = true,
                MinSessions = 1,
                MaxSessions = 100,
                DefaultSessionDurationMinutes = 60,
                MinGroupSize = 1,
                MaxGroupSize = 20,
                AllowExtension = true,
                AllowFlexibleCourses = true,
                CreatedAt = now
            }
        };

    private static async Task EnsureShariaDomainAsync(ApplicationDBContext context)
    {
        var existing = await context.EducationDomains
            .Include(d => d.EducationRule)
            .FirstOrDefaultAsync(d => d.Code == EducationDomainCodes.Sharia);
        if (existing is null)
        {
            await context.EducationDomains.AddAsync(CreateShariaDomain(DateTime.UtcNow));
            await context.SaveChangesAsync();
            return;
        }

        var rule = existing.EducationRule;
        if (rule is null)
            return;

        var dirty = false;
        if (!rule.HasParentSubject) { rule.HasParentSubject = true; dirty = true; }
        if (!rule.HasWritableFilters) { rule.HasWritableFilters = true; dirty = true; }
        if (!rule.HasEducationLevel) { rule.HasEducationLevel = true; dirty = true; }
        if (!rule.EducationLevelAfterSubject) { rule.EducationLevelAfterSubject = true; dirty = true; }
        if (!rule.HasContentUnits) { rule.HasContentUnits = true; dirty = true; }
        if (!rule.HasLessons) { rule.HasLessons = true; dirty = true; }
        if (!rule.RulesConfigured) { rule.RulesConfigured = true; dirty = true; }
        if (dirty)
        {
            rule.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureExcelCoreDomainsAsync(ApplicationDBContext context)
    {
        var domains = await context.EducationDomains
            .Include(d => d.EducationRule)
            .Where(d => d.Code == "school" || d.Code == "university" || d.Code == "quran" || d.Code == "language")
            .ToListAsync();

        var dirtyAny = false;
        foreach (var domain in domains)
        {
            var rule = domain.EducationRule;
            if (rule is null)
                continue;

            var dirty = false;
            if (!rule.RulesConfigured) { rule.RulesConfigured = true; dirty = true; }

            switch (domain.Code)
            {
                case "quran":
                    if (!rule.HasEducationLevel) { rule.HasEducationLevel = true; dirty = true; }
                    if (!rule.HasWritableFilters) { rule.HasWritableFilters = true; dirty = true; }
                    if (rule.HasLessons) { rule.HasLessons = false; dirty = true; }
                    if (rule.RequiresQuranLevel) { rule.RequiresQuranLevel = false; dirty = true; }
                    if (!rule.RequiresQuranContentType) { rule.RequiresQuranContentType = true; dirty = true; }
                    if (!rule.HasContentUnits) { rule.HasContentUnits = true; dirty = true; }
                    break;
                case "language":
                    if (!rule.HasEducationLevel) { rule.HasEducationLevel = true; dirty = true; }
                    if (!rule.HasGrade) { rule.HasGrade = true; dirty = true; }
                    if (!rule.HasWritableFilters) { rule.HasWritableFilters = true; dirty = true; }
                    if (!rule.HasContentUnits) { rule.HasContentUnits = true; dirty = true; }
                    if (!rule.HasLessons) { rule.HasLessons = true; dirty = true; }
                    break;
                case "school":
                    if (!rule.HasContentUnits) { rule.HasContentUnits = true; dirty = true; }
                    if (!rule.HasLessons) { rule.HasLessons = true; dirty = true; }
                    break;
                case "university":
                    if (!rule.HasContentUnits) { rule.HasContentUnits = true; dirty = true; }
                    if (!rule.HasLessons) { rule.HasLessons = true; dirty = true; }
                    break;
            }

            if (dirty)
            {
                rule.UpdatedAt = DateTime.UtcNow;
                dirtyAny = true;
            }
        }

        if (dirtyAny)
            await context.SaveChangesAsync();
    }
}

