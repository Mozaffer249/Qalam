using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Legal;

namespace Qalam.Infrastructure.Configurations.Legal;

public class LegalDocumentSectionConfiguration : IEntityTypeConfiguration<LegalDocumentSection>
{
    public void Configure(EntityTypeBuilder<LegalDocumentSection> builder)
    {
        builder.ToTable("LegalDocumentSections", "legal");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AnchorKey).IsRequired().HasMaxLength(100);
        builder.Property(e => e.TitleAr).IsRequired().HasMaxLength(300);
        builder.Property(e => e.TitleEn).IsRequired().HasMaxLength(300);
        builder.Property(e => e.ContentAr).HasColumnType("nvarchar(max)");
        builder.Property(e => e.ContentEn).HasColumnType("nvarchar(max)");
        builder.Property(e => e.IsEnabled).HasDefaultValue(true);

        builder.HasIndex(e => e.LegalDocumentVersionId);
        builder.HasIndex(e => e.ParentSectionId);
        builder.HasIndex(e => new { e.LegalDocumentVersionId, e.AnchorKey }).IsUnique();
        builder.HasIndex(e => new { e.LegalDocumentVersionId, e.ParentSectionId, e.DisplayOrder });

        builder.HasOne(e => e.ParentSection)
            .WithMany(e => e.ChildSections)
            .HasForeignKey(e => e.ParentSectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
