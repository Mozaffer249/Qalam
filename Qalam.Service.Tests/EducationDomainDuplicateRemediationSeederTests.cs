using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeding;

namespace Qalam.Service.Tests;

public class EducationDomainDuplicateRemediationSeederTests
{
    private static ApplicationDBContext CreateDb(string? databaseName = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EncryptionSettings:Key"] = "0123456789abcdef0123456789abcdef",
            })
            .Build();
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDBContext(options, config);
    }

    private static EducationDomain Domain(
        int id,
        string code,
        string nameAr,
        string nameEn,
        DateTime createdAt,
        Action<EducationRule>? configureRule = null)
    {
        var rule = new EducationRule { CreatedAt = createdAt, RulesConfigured = true };
        configureRule?.Invoke(rule);
        return new EducationDomain
        {
            Id = id,
            Code = code,
            NameAr = nameAr,
            NameEn = nameEn,
            IsActive = true,
            CreatedAt = createdAt,
            EducationRule = rule,
        };
    }

    private static TeacherDomainQuestion CustomQ(
        int domainId,
        string code,
        bool isActive,
        DateTime? createdAt = null) =>
        new()
        {
            DomainId = domainId,
            Code = code,
            NameAr = code,
            NameEn = code,
            IsActive = isActive,
            IsSystem = false,
            RequirementType = Qalam.Data.Entity.Common.Enums.RegistrationRequirementType.Text,
            SortOrder = 1,
            CreatedAt = createdAt ?? DateTime.UtcNow,
        };

    private static TeacherDomainQuestion SystemQ(int domainId, string code) =>
        new()
        {
            DomainId = domainId,
            Code = code,
            NameAr = code,
            NameEn = code,
            IsActive = true,
            IsSystem = true,
            RequirementType = Qalam.Data.Entity.Common.Enums.RegistrationRequirementType.Text,
            SortOrder = 10,
            CreatedAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task Merges_NameAr_Twins_Into_Older_With_Custom_Questions()
    {
        await using var db = CreateDb();
        var older = Domain(
            1,
            "school-custom",
            "تعليم مدرسي",
            "School",
            DateTime.UtcNow.AddDays(-30),
            r => r.HasCurriculum = true);
        var newer = Domain(
            2,
            "school",
            "تعليم مدرسي",
            "School Education",
            DateTime.UtcNow.AddDays(-1),
            r =>
            {
                r.HasCurriculum = true;
                r.HasEducationLevel = true;
                r.HasGrade = true;
                r.HasWritableFilters = true;
            });
        db.EducationDomains.AddRange(older, newer);
        db.TeacherDomainQuestions.Add(CustomQ(1, "custom_q", isActive: true));
        db.Subjects.Add(new Subject
        {
            DomainId = 2,
            Code = "math",
            NameAr = "رياضيات",
            NameEn = "Math",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await EducationDomainDuplicateRemediationSeeder.SeedAsync(db);

        var keeper = await db.EducationDomains.Include(d => d.EducationRule).SingleAsync(d => d.Id == 1);
        var donor = await db.EducationDomains.SingleAsync(d => d.Id == 2);

        Assert.True(keeper.IsActive);
        Assert.Equal("school", keeper.Code);
        Assert.Equal("تعليم مدرسي", keeper.NameAr);
        Assert.True(keeper.EducationRule!.HasGrade);
        Assert.False(donor.IsActive);
        Assert.Contains("archive", donor.Code, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("أرشيف", donor.NameAr);

        var subject = await db.Subjects.SingleAsync();
        Assert.Equal(1, subject.DomainId);
    }

    [Fact]
    public async Task Keeps_Wave1_Without_Twin()
    {
        await using var db = CreateDb();
        db.EducationDomains.Add(Domain(
            1,
            EducationDomainCodes.SoftSkills,
            "المهارات العملية والناعمة",
            "Soft",
            DateTime.UtcNow,
            r => r.HasParentSubject = true));
        await db.SaveChangesAsync();

        await EducationDomainDuplicateRemediationSeeder.SeedAsync(db);

        var domain = await db.EducationDomains.SingleAsync();
        Assert.True(domain.IsActive);
        Assert.Equal(EducationDomainCodes.SoftSkills, domain.Code);
    }

    [Fact]
    public async Task Explicit_SoftSkills_Pair_Keeps_Legacy_And_Archives_Seed()
    {
        await using var db = CreateDb();
        // txfiles: Id 10 csacscd (trailing space on NameAr) + Id 1010 soft-skills
        db.EducationDomains.AddRange(
            Domain(
                10,
                "csacscd",
                "المهارات العملية والناعمة ",
                "practical and soft skills",
                new DateTime(2026, 7, 11),
                r => r.HasCurriculum = true),
            Domain(
                1010,
                EducationDomainCodes.SoftSkills,
                "المهارات العملية والناعمة",
                "Practical and Soft Skills",
                new DateTime(2026, 8, 25),
                r =>
                {
                    r.HasParentSubject = true;
                    r.HasWritableFilters = true;
                }));
        db.TeacherDomainQuestions.Add(CustomQ(10, "skills", isActive: true));
        db.TeacherDomainQuestions.Add(SystemQ(1010, "soft_skills_experience_years"));
        db.Subjects.Add(new Subject
        {
            DomainId = 1010,
            Code = "soft-parent",
            NameAr = "فئة",
            NameEn = "Cat",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await EducationDomainDuplicateRemediationSeeder.SeedAsync(db);

        var keeper = await db.EducationDomains.Include(d => d.EducationRule).SingleAsync(d => d.Id == 10);
        var donor = await db.EducationDomains.SingleAsync(d => d.Id == 1010);

        Assert.True(keeper.IsActive);
        Assert.Equal(EducationDomainCodes.SoftSkills, keeper.Code);
        Assert.True(keeper.EducationRule!.HasParentSubject);
        Assert.True(keeper.EducationRule.HasWritableFilters);
        Assert.False(donor.IsActive);
        Assert.Contains("archive", donor.Code, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(10, (await db.Subjects.SingleAsync()).DomainId);
        Assert.True(await db.TeacherDomainQuestions.AnyAsync(q =>
            q.DomainId == 10 && q.Code == "skills" && q.IsActive && !q.IsSystem));
    }

    [Fact]
    public async Task Explicit_University_Pair_Keeps_Ititit_Over_System_University()
    {
        await using var db = CreateDb();
        // txfiles: Id 12 ititit (custom) + Id 5 university (system only)
        db.EducationDomains.AddRange(
            Domain(
                5,
                EducationDomainCodes.University,
                "التعليم الجامعي",
                "University Education",
                new DateTime(2026, 5, 16),
                r =>
                {
                    r.HasUniversity = true;
                    r.HasCollege = true;
                }),
            Domain(
                12,
                "ititit",
                "التعليم الجامعي",
                "Higher Education",
                new DateTime(2026, 7, 11),
                r => r.HasCurriculum = true));
        db.TeacherDomainQuestions.Add(CustomQ(12, "experience_years", isActive: true));
        db.TeacherDomainQuestions.Add(SystemQ(5, "university_teaching_experience"));
        await db.SaveChangesAsync();

        await EducationDomainDuplicateRemediationSeeder.SeedAsync(db);

        var keeper = await db.EducationDomains.Include(d => d.EducationRule).SingleAsync(d => d.Id == 12);
        var donor = await db.EducationDomains.SingleAsync(d => d.Id == 5);

        Assert.True(keeper.IsActive);
        Assert.Equal(EducationDomainCodes.University, keeper.Code);
        Assert.True(keeper.EducationRule!.HasUniversity);
        Assert.True(keeper.EducationRule.HasCollege);
        Assert.False(donor.IsActive);
        Assert.True(await db.TeacherDomainQuestions.AnyAsync(q =>
            q.DomainId == 12 && q.Code == "experience_years" && q.IsActive));
    }

    [Fact]
    public async Task Explicit_Knowledge_Pair_Reactivates_Inactive_Custom_Questions()
    {
        await using var db = CreateDb();
        // txfiles: Id 4 skills (inactive customs) + Id 1015 knowledge (system)
        db.EducationDomains.AddRange(
            Domain(
                4,
                EducationDomainCodes.Skills,
                "العلوم والثقافة والمعرفة",
                "Science and Knowledge",
                new DateTime(2026, 5, 16)),
            Domain(
                1015,
                EducationDomainCodes.Knowledge,
                "العلوم والثقافة والمعرفة",
                "Science, Culture, and Knowledge",
                new DateTime(2026, 8, 25),
                r => r.HasParentSubject = true));
        db.TeacherDomainQuestions.Add(CustomQ(4, "academic_q", isActive: false));
        db.TeacherDomainQuestions.Add(CustomQ(4, "since12", isActive: false));
        db.TeacherDomainQuestions.Add(SystemQ(1015, "knowledge_experience_years"));
        await db.SaveChangesAsync();

        await EducationDomainDuplicateRemediationSeeder.SeedAsync(db);

        var keeper = await db.EducationDomains.Include(d => d.EducationRule).SingleAsync(d => d.Id == 4);
        var donor = await db.EducationDomains.SingleAsync(d => d.Id == 1015);

        Assert.True(keeper.IsActive);
        Assert.Equal(EducationDomainCodes.Knowledge, keeper.Code);
        Assert.True(keeper.EducationRule!.HasParentSubject);
        Assert.False(donor.IsActive);

        var customs = await db.TeacherDomainQuestions
            .Where(q => q.DomainId == 4 && !q.IsSystem)
            .ToListAsync();
        Assert.Equal(2, customs.Count);
        Assert.All(customs, q => Assert.True(q.IsActive));
    }

    [Fact]
    public async Task Deactivates_Try_Domains()
    {
        await using var db = CreateDb();
        db.EducationDomains.AddRange(
            Domain(14, "try_1", "تجربة للمشكلة", "try", DateTime.UtcNow.AddDays(-10)),
            Domain(15, "try_88", "تجربة للمشكلة الحاصلة", "try try", DateTime.UtcNow.AddDays(-9)),
            Domain(1, EducationDomainCodes.School, "التعليم المدرسي", "School", DateTime.UtcNow.AddDays(-30)));
        await db.SaveChangesAsync();

        await EducationDomainDuplicateRemediationSeeder.SeedAsync(db);

        Assert.False((await db.EducationDomains.SingleAsync(d => d.Id == 14)).IsActive);
        Assert.False((await db.EducationDomains.SingleAsync(d => d.Id == 15)).IsActive);
        Assert.True((await db.EducationDomains.SingleAsync(d => d.Id == 1)).IsActive);
    }

    [Fact]
    public async Task Remap_Dedups_Curriculum_NameEn_And_Drops_Duplicate_Approvals()
    {
        await using var db = CreateDb();
        db.EducationDomains.AddRange(
            Domain(10, "csacscd", "المهارات العملية والناعمة", "soft", new DateTime(2026, 7, 11)),
            Domain(1010, EducationDomainCodes.SoftSkills, "المهارات العملية والناعمة", "Soft Skills", new DateTime(2026, 8, 25),
                r => r.HasParentSubject = true));
        db.TeacherDomainQuestions.Add(CustomQ(10, "skills", isActive: true));
        db.Curriculums.AddRange(
            new Curriculum
            {
                DomainId = 10,
                NameAr = "عام",
                NameEn = "General",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new Curriculum
            {
                DomainId = 1010,
                NameAr = "عام",
                NameEn = "General",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        db.TeacherDomainApprovals.AddRange(
            new TeacherDomainApproval
            {
                TeacherId = 1,
                DomainId = 10,
                ApprovedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            },
            new TeacherDomainApproval
            {
                TeacherId = 1,
                DomainId = 1010,
                ApprovedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            },
            new TeacherDomainApproval
            {
                TeacherId = 2,
                DomainId = 1010,
                ApprovedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        await EducationDomainDuplicateRemediationSeeder.SeedAsync(db);

        Assert.Equal(EducationDomainCodes.SoftSkills,
            (await db.EducationDomains.SingleAsync(d => d.Id == 10)).Code);
        Assert.False((await db.EducationDomains.SingleAsync(d => d.Id == 1010)).IsActive);

        var curricula = await db.Curriculums.Where(c => c.DomainId == 10).ToListAsync();
        Assert.Equal(2, curricula.Count);
        Assert.Contains(curricula, c => c.NameEn == "General");
        Assert.Contains(curricula, c => c.NameEn.Contains("-dup-", StringComparison.Ordinal));

        var approvals = await db.TeacherDomainApprovals.Where(a => a.DomainId == 10).ToListAsync();
        Assert.Equal(2, approvals.Count);
        Assert.DoesNotContain(await db.TeacherDomainApprovals.ToListAsync(), a => a.DomainId == 1010);
        Assert.Contains(approvals, a => a.TeacherId == 1);
        Assert.Contains(approvals, a => a.TeacherId == 2);
    }
}
