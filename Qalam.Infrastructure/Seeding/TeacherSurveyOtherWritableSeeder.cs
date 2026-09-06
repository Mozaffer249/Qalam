using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Teacher routes-builder: school/university «أخرى» write-ins + soft.other_field backfill.
/// </summary>
public static class TeacherSurveyOtherWritableSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        await EnsureDomainWritableFlagsAsync(context);
        await EnsureSchoolOtherAsync(context);
        await EnsureUniversityOtherAsync(context);
        await EnsureSoftOtherFieldAsync(context);
    }

    private static async Task EnsureDomainWritableFlagsAsync(ApplicationDBContext context)
    {
        var domains = await context.EducationDomains
            .Include(d => d.EducationRule)
            .Where(d => d.Code == EducationDomainCodes.School || d.Code == EducationDomainCodes.University)
            .ToListAsync();

        var dirty = false;
        foreach (var domain in domains)
        {
            if (domain.EducationRule is null) continue;
            if (!domain.EducationRule.HasWritableFilters)
            {
                domain.EducationRule.HasWritableFilters = true;
                domain.EducationRule.UpdatedAt = DateTime.UtcNow;
                dirty = true;
            }
        }

        if (dirty)
            await context.SaveChangesAsync();
    }

    private static async Task EnsureSchoolOtherAsync(ApplicationDBContext context)
    {
        var domain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == EducationDomainCodes.School);
        if (domain is null) return;

        var curriculumId = await context.Curriculums
            .Where(c => c.DomainId == domain.Id)
            .OrderBy(c => c.Id)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        if (!await context.Subjects.AnyAsync(s =>
                s.DomainId == domain.Id && s.Code == "school.other"))
        {
            context.Subjects.Add(new Subject
            {
                DomainId = domain.Id,
                CurriculumId = curriculumId,
                Code = "school.other",
                NameAr = "أخرى",
                NameEn = "Other",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        await EnsureSlotAsync(
            context,
            domain.Id,
            WritableFilterSlotCodes.SchoolOtherSubject,
            "مادة أخرى",
            "Other subject",
            WritableFilterAfterSteps.Subject,
            order: 1,
            requiredWhen: ".other");
    }

    private static async Task EnsureUniversityOtherAsync(ApplicationDBContext context)
    {
        var domain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == EducationDomainCodes.University);
        if (domain is null) return;

        if (!await context.Subjects.AnyAsync(s =>
                s.DomainId == domain.Id && s.Code == "university.other"))
        {
            context.Subjects.Add(new Subject
            {
                DomainId = domain.Id,
                Code = "university.other",
                NameAr = "أخرى",
                NameEn = "Other",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var specs = new (string Code, string Ar, string En, string After, int Order, string? When)[]
        {
            (WritableFilterSlotCodes.UniversityOtherUniversity, "جامعة أخرى", "Other university",
                WritableFilterAfterSteps.Subject, 1, null),
            (WritableFilterSlotCodes.UniversityOtherCollege, "كلية أخرى", "Other college",
                WritableFilterAfterSteps.Subject, 2, null),
            (WritableFilterSlotCodes.UniversityOtherMajor, "تخصص آخر", "Other major",
                WritableFilterAfterSteps.Subject, 3, null),
            (WritableFilterSlotCodes.UniversityOtherCourse, "مقرر آخر", "Other course",
                WritableFilterAfterSteps.Subject, 4, ".other"),
        };

        foreach (var spec in specs)
        {
            await EnsureSlotAsync(
                context,
                domain.Id,
                spec.Code,
                spec.Ar,
                spec.En,
                spec.After,
                spec.Order,
                spec.When);
        }
    }

    private static async Task EnsureSoftOtherFieldAsync(ApplicationDBContext context)
    {
        var domain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == EducationDomainCodes.SoftSkills);
        if (domain is null) return;

        await EnsureSlotAsync(
            context,
            domain.Id,
            WritableFilterSlotCodes.SoftOtherField,
            "مجال آخر",
            "Other field",
            WritableFilterAfterSteps.ParentSubject,
            order: 1,
            requiredWhen: ".other");

        var skillSlot = await context.WritableFilterSlots
            .FirstOrDefaultAsync(s =>
                s.DomainId == domain.Id && s.Code == WritableFilterSlotCodes.SoftOtherSkill);
        if (skillSlot is not null && skillSlot.RequiredWhenSubjectCodeContains is not null)
        {
            skillSlot.RequiredWhenSubjectCodeContains = null;
            skillSlot.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureSlotAsync(
        ApplicationDBContext context,
        int domainId,
        string code,
        string nameAr,
        string nameEn,
        string afterStep,
        int order,
        string? requiredWhen)
    {
        if (await context.WritableFilterSlots.AnyAsync(s => s.DomainId == domainId && s.Code == code))
            return;

        context.WritableFilterSlots.Add(new WritableFilterSlot
        {
            DomainId = domainId,
            Code = code,
            NameAr = nameAr,
            NameEn = nameEn,
            AfterStep = afterStep,
            OrderIndex = order,
            IsRequired = false,
            RequiredWhenSubjectCodeContains = requiredWhen,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }
}
