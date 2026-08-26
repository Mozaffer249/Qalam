using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeding;

namespace Qalam.Service.Tests;

public class ShariaCatalogSeederTests
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
    public async Task Backfills_Sharia_Excel_Tree_When_Legacy_Flat_Subject_Exists()
    {
        await using var db = CreateDb();
        db.EducationDomains.Add(new EducationDomain
        {
            Code = EducationDomainCodes.Sharia,
            NameAr = "علوم الشريعة",
            NameEn = "Sharia",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        var domainId = (await db.EducationDomains.SingleAsync()).Id;

        db.Subjects.Add(new Subject
        {
            DomainId = domainId,
            Code = "legacy-sharia-flat",
            NameAr = "قديم",
            NameEn = "Old",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await ShariaCatalogSeeder.SeedAsync(db);

        Assert.True(await db.Subjects.AnyAsync(s =>
            s.DomainId == domainId && s.Code == "sharia.category.sharia-sciences"));
        Assert.True(await db.Subjects.AnyAsync(s =>
            s.DomainId == domainId && s.Code == "sharia.spec.fiqh"));
        Assert.True(await db.Subjects.AnyAsync(s =>
            s.DomainId == domainId && s.Code == "legacy-sharia-flat"));
    }
}
