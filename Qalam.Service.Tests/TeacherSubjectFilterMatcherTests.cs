using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Queries;

namespace Qalam.Service.Tests;

public class TeacherSubjectFilterMatcherTests
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
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDBContext(options, config);
    }

    [Fact]
    public async Task LevelId_MatchesEducationLevelJunction_NotOnlySubjectLevel()
    {
        await using var db = CreateDb();
        var domain = new EducationDomain { Id = 1, Code = "language", NameAr = "لغات", NameEn = "Language", IsActive = true };
        var level = new EducationLevel { Id = 10, NameAr = "A1", NameEn = "A1" };
        var subject = new Subject
        {
            Id = 100,
            DomainId = 1,
            Domain = domain,
            NameAr = "English",
            NameEn = "English",
            LevelId = null,
        };
        var teacherSubject = new TeacherSubject
        {
            Id = 1,
            TeacherId = 1,
            SubjectId = 100,
            Subject = subject,
            IsActive = true,
            EducationLevels =
            {
                new TeacherSubjectEducationLevel { EducationLevelId = 10, EducationLevel = level },
            },
        };
        db.AddRange(domain, level, subject, teacherSubject);
        await db.SaveChangesAsync();

        var filters = new TeacherSubjectDiscoverFilters { LevelId = 10 };
        var ids = await db.Set<TeacherSubject>()
            .Where(ts => ts.IsActive)
            .ApplyDiscoverFilters(filters)
            .Select(ts => ts.Id)
            .ToListAsync();

        Assert.Contains(1, ids);
    }

    [Fact]
    public async Task GradeId_MatchesGradeJunction()
    {
        await using var db = CreateDb();
        var domain = new EducationDomain { Id = 2, Code = "language", NameAr = "لغات", NameEn = "Language", IsActive = true };
        var grade = new Grade { Id = 20, NameAr = "B2", NameEn = "B2" };
        var subject = new Subject
        {
            Id = 101,
            DomainId = 2,
            Domain = domain,
            NameAr = "French",
            NameEn = "French",
            GradeId = null,
        };
        var teacherSubject = new TeacherSubject
        {
            Id = 2,
            TeacherId = 2,
            SubjectId = 101,
            Subject = subject,
            IsActive = true,
            Grades = { new TeacherSubjectGrade { GradeId = 20, Grade = grade } },
        };
        db.AddRange(domain, grade, subject, teacherSubject);
        await db.SaveChangesAsync();

        var filters = new TeacherSubjectDiscoverFilters { GradeId = 20 };
        var count = await db.Set<TeacherSubject>()
            .Where(ts => ts.IsActive)
            .ApplyDiscoverFilters(filters)
            .CountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WritableFilterValueIds_MatchesTeacherWritableCoverage()
    {
        await using var db = CreateDb();
        var domain = new EducationDomain { Id = 3, Code = "soft-skills", NameAr = "مهارات", NameEn = "Skills", IsActive = true };
        var writable = new WritableFilterValue
        {
            Id = 30,
            NameAr = "قيادة",
            NameEn = "Leadership",
            NormalizedText = "leadership",
        };
        var subject = new Subject { Id = 102, DomainId = 3, Domain = domain, NameAr = "Soft", NameEn = "Soft" };
        var teacherSubject = new TeacherSubject
        {
            Id = 3,
            TeacherId = 3,
            SubjectId = 102,
            Subject = subject,
            IsActive = true,
            WritableFilters = { new TeacherSubjectWritableFilter { WritableFilterValueId = 30, WritableFilterValue = writable } },
        };
        db.AddRange(domain, writable, subject, teacherSubject);
        await db.SaveChangesAsync();

        var filters = new TeacherSubjectDiscoverFilters { WritableFilterValueIds = new List<int> { 30 } };
        var count = await db.Set<TeacherSubject>()
            .Where(ts => ts.IsActive)
            .ApplyDiscoverFilters(filters)
            .CountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task FieldLevelPairs_MatchesFinanceCoverage()
    {
        await using var db = CreateDb();
        var domain = new EducationDomain { Id = 4, Code = "finance", NameAr = "مال", NameEn = "Finance", IsActive = true };
        var writable = new WritableFilterValue
        {
            Id = 40,
            NameAr = "استثمار",
            NameEn = "Investing",
            NormalizedText = "investing",
        };
        var level = new EducationLevel { Id = 41, NameAr = "متوسط", NameEn = "Intermediate" };
        var subject = new Subject { Id = 103, DomainId = 4, Domain = domain, NameAr = "Finance", NameEn = "Finance" };
        var teacherSubject = new TeacherSubject
        {
            Id = 4,
            TeacherId = 4,
            SubjectId = 103,
            Subject = subject,
            IsActive = true,
            FieldLevels =
            {
                new TeacherSubjectFieldLevel
                {
                    WritableFilterValueId = 40,
                    WritableFilterValue = writable,
                    EducationLevelId = 41,
                    EducationLevel = level,
                },
            },
        };
        db.AddRange(domain, writable, level, subject, teacherSubject);
        await db.SaveChangesAsync();

        var filters = new TeacherSubjectDiscoverFilters
        {
            FieldLevelPairs = new List<FieldLevelPairFilter>
            {
                new() { WritableFilterValueId = 40, EducationLevelId = 41 },
            },
        };
        var count = await db.Set<TeacherSubject>()
            .Where(ts => ts.IsActive)
            .ApplyDiscoverFilters(filters)
            .CountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task QuranLevelIds_MatchesEducationLevelAudience()
    {
        await using var db = CreateDb();
        var domain = new EducationDomain { Id = 5, Code = "quran", NameAr = "قرآن", NameEn = "Quran", IsActive = true };
        var audience = new EducationLevel { Id = 50, NameAr = "كبار", NameEn = "Adults" };
        var subject = new Subject { Id = 104, DomainId = 5, Domain = domain, NameAr = "Quran", NameEn = "Quran" };
        var teacherSubject = new TeacherSubject
        {
            Id = 5,
            TeacherId = 5,
            SubjectId = 104,
            Subject = subject,
            IsActive = true,
            EducationLevels = { new TeacherSubjectEducationLevel { EducationLevelId = 50, EducationLevel = audience } },
        };
        db.AddRange(domain, audience, subject, teacherSubject);
        await db.SaveChangesAsync();

        var filters = new TeacherSubjectDiscoverFilters { QuranLevelIds = new List<int> { 50 } };
        var count = await db.Set<TeacherSubject>()
            .Where(ts => ts.IsActive)
            .ApplyDiscoverFilters(filters)
            .CountAsync();

        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("school")]
    [InlineData("university")]
    [InlineData("quran")]
    [InlineData("sharia")]
    [InlineData("language")]
    [InlineData("tech-skills")]
    [InlineData("soft-skills")]
    [InlineData("life-skills")]
    [InlineData("hobbies")]
    [InlineData("finance")]
    [InlineData("knowledge")]
    public void HasAnyDiscoverFilters_IsFalseForEmptyFilters(string domainCode)
    {
        _ = domainCode;
        var filters = new TeacherSubjectDiscoverFilters();
        Assert.False(TeacherSubjectFilterMatcher.HasAnyDiscoverFilters(filters));
    }

    [Fact]
    public void HasAnyDiscoverFilters_IsTrueWhenAnyParamSet()
    {
        Assert.True(TeacherSubjectFilterMatcher.HasAnyDiscoverFilters(new TeacherSubjectDiscoverFilters { DomainId = 1 }));
        Assert.True(TeacherSubjectFilterMatcher.HasAnyDiscoverFilters(new TeacherSubjectDiscoverFilters { WritableFilterValueIds = new List<int> { 1 } }));
        Assert.True(TeacherSubjectFilterMatcher.HasAnyDiscoverFilters(new TeacherSubjectDiscoverFilters
        {
            FieldLevelPairs = new List<FieldLevelPairFilter> { new() { WritableFilterValueId = 1, EducationLevelId = 2 } },
        }));
    }
}
