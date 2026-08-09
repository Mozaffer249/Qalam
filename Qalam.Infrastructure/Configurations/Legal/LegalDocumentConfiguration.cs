using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Legal;

namespace Qalam.Infrastructure.Configurations.Legal;

public class LegalDocumentConfiguration : IEntityTypeConfiguration<LegalDocument>
{
    public void Configure(EntityTypeBuilder<LegalDocument> builder)
    {
        builder.ToTable("LegalDocuments", "legal");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
        builder.Property(e => e.TitleAr).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TitleEn).IsRequired().HasMaxLength(200);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.RequiresConsent).HasDefaultValue(false);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.DisplayOrder);

        builder.HasOne(e => e.CurrentPublishedVersion)
            .WithMany()
            .HasForeignKey(e => e.CurrentPublishedVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Versions)
            .WithOne(v => v.LegalDocument)
            .HasForeignKey(v => v.LegalDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
