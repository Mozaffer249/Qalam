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

    private static async Task<int> SeedShariaDomainAsync(ApplicationDBContext db)
    {
        db.EducationDomains.Add(new EducationDomain
        {
            Code = EducationDomainCodes.Sharia,
            NameAr = "علوم الشريعة",
            NameEn = "Sharia",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return (await db.EducationDomains.SingleAsync()).Id;
    }

    [Fact]
    public async Task Backfills_Sharia_Excel_Tree_When_Legacy_Flat_Subject_Exists()
    {
        await using var db = CreateDb();
        var domainId = await SeedShariaDomainAsync(db);

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
        Assert.False(await db.Subjects.AnyAsync(s =>
            s.DomainId == domainId && s.Code == "legacy-sharia-flat" && s.IsActive));
    }

    [Fact]
    public async Task Ensures_Five_Excel_Levels_When_One_Legacy_Level_Exists()
    {
        await using var db = CreateDb();
        var domainId = await SeedShariaDomainAsync(db);

        db.EducationLevels.Add(new EducationLevel
        {
            DomainId = domainId,
            NameAr = "قديم",
            NameEn = "Legacy level",
            OrderIndex = 99,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await ShariaCatalogSeeder.SeedAsync(db);

        var activeLevels = await db.EducationLevels
            .Where(l => l.DomainId == domainId && l.IsActive)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync();

        Assert.Equal(6, activeLevels.Count);
        Assert.Equal("Beginner students of knowledge", activeLevels[0].NameEn);
        Assert.Equal("New Muslims", activeLevels[4].NameEn);
        Assert.Equal("Legacy level", activeLevels[5].NameEn);
    }

    [Fact]
    public async Task Deactivates_Dup_Writable_Slots_When_Excel_Catalog_Exists()
    {
        await using var db = CreateDb();
        var domainId = await SeedShariaDomainAsync(db);

        db.Subjects.Add(new Subject
        {
            DomainId = domainId,
            Code = "sharia.category.sharia-sciences",
            NameAr = "العلوم الشرعية",
            NameEn = "Sharia sciences",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        db.WritableFilterSlots.Add(new WritableFilterSlot
        {
            DomainId = domainId,
            Code = "sharia.education_type-dup-1016",
            NameAr = "قديم",
            NameEn = "Old education type",
            AfterStep = WritableFilterAfterSteps.Subject,
            OrderIndex = 1,
            IsRequired = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await ShariaCatalogSeeder.SeedAsync(db);

        Assert.True(await db.WritableFilterSlots.AnyAsync(s =>
            s.DomainId == domainId &&
            s.Code == WritableFilterSlotCodes.ShariaEducationType &&
            s.IsActive));
        Assert.True(await db.WritableFilterSlots.AnyAsync(s =>
            s.DomainId == domainId &&
            s.Code == WritableFilterSlotCodes.ShariaBook &&
            s.IsActive));
        Assert.False(await db.WritableFilterSlots.AnyAsync(s =>
            s.DomainId == domainId &&
            s.Code == "sharia.education_type-dup-1016" &&
            s.IsActive));
    }

    [Fact]
    public async Task Active_Root_Subjects_Are_Excel_Categories_Only_After_Seed()
    {
        await using var db = CreateDb();
        var domainId = await SeedShariaDomainAsync(db);

        db.Subjects.Add(new Subject
        {
            DomainId = domainId,
            Code = "legacy-admin-root",
            NameAr = "مادة قديمة",
            NameEn = "Legacy admin subject",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await ShariaCatalogSeeder.SeedAsync(db);

        var activeRoots = await db.Subjects
            .Where(s => s.DomainId == domainId && s.ParentSubjectId == null && s.IsActive)
            .OrderBy(s => s.Code)
            .ToListAsync();

        Assert.Equal(2, activeRoots.Count);
        Assert.All(activeRoots, s => Assert.StartsWith("sharia.category.", s.Code, StringComparison.OrdinalIgnoreCase));
    }
}
