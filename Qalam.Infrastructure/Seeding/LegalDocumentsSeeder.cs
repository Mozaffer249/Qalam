using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeding.Data;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Seeds initial published legal documents (terms, privacy, refund, pricing) from <see cref="LegalDocumentSeedData"/>.
/// Idempotent: skips when the table is missing or a document Code already exists.
/// </summary>
public static class LegalDocumentsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "legal", "LegalDocuments"))
            return;

        var now = DateTime.UtcNow;

        foreach (var seed in LegalDocumentSeedData.GetDocuments())
        {
            var exists = await context.LegalDocuments.AnyAsync(d => d.Code == seed.Code);
            if (exists)
                continue;

            var document = new LegalDocument
            {
                Code = seed.Code,
                TitleAr = seed.TitleAr,
                TitleEn = seed.TitleEn,
                DisplayOrder = seed.DisplayOrder,
                IsActive = true,
                RequiresConsent = seed.RequiresConsent,
                CreatedAt = now,
            };

            var version = new LegalDocumentVersion
            {
                MajorVersion = 1,
                MinorVersion = 0,
                Status = LegalDocumentStatus.Published,
                EffectiveDate = now,
                PublishedAt = now,
                CreatedAt = now,
                LegalDocument = document,
            };

            document.Versions.Add(version);

            foreach (var sectionSeed in seed.Sections)
                AddSection(version, sectionSeed, parent: null, now);

            await context.LegalDocuments.AddAsync(document);
            await context.SaveChangesAsync();

            document.CurrentPublishedVersionId = version.Id;
            await context.SaveChangesAsync();
        }
    }

    private static void AddSection(
        LegalDocumentVersion version,
        LegalSeedSection seed,
        LegalDocumentSection? parent,
        DateTime now)
    {
        var section = new LegalDocumentSection
        {
            AnchorKey = seed.AnchorKey,
            TitleAr = seed.TitleAr,
            TitleEn = seed.TitleEn,
            ContentAr = seed.ContentAr,
            ContentEn = seed.ContentEn,
            DisplayOrder = seed.DisplayOrder,
            IsEnabled = true,
            CreatedAt = now,
            LegalDocumentVersion = version,
            ParentSection = parent,
        };

        version.Sections.Add(section);

        if (seed.Children is { Count: > 0 })
        {
            foreach (var child in seed.Children)
                AddSection(version, child, section, now);
        }
    }
}
