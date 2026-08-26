using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeding;

namespace Qalam.Service.Tests;

public class SkillsSplitCatalogSeederTests
{
    private static ApplicationDBContext CreateDb()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EncryptionSettings:Key"] = "0123456789abcdef0123456789abcdef",
            })
            .Build();
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDBContext(options, config);
    }

    [Fact]
    public async Task Backfills_LifeSkills_Excel_Tree_When_Legacy_Flat_Subject_Exists()
    {
        await using var db = CreateDb();
        var domain = new EducationDomain
        {
            Code = EducationDomainCodes.LifeSkills,
            NameAr = "المهارات الحياتية",
            NameEn = "Life Skills",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        db.EducationDomains.Add(domain);
        await db.SaveChangesAsync();

        db.Subjects.Add(new Subject
        {
            DomainId = domain.Id,
            Code = "legacy-admin-subject",
            NameAr = "مادة قديمة",
            NameEn = "Legacy",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await SkillsSplitCatalogSeeder.SeedAsync(db);

        Assert.True(await db.Subjects.AnyAsync(s =>
            s.DomainId == domain.Id && s.Code == "life.self" && s.ParentSubjectId == null));
        Assert.True(await db.Subjects.AnyAsync(s =>
            s.DomainId == domain.Id && s.Code == "life.self.confidence"));
        Assert.True(await db.Subjects.AnyAsync(s =>
            s.DomainId == domain.Id && s.Code == "legacy-admin-subject"));
    }
}
