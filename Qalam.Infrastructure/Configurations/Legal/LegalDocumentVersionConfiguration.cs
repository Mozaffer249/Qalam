using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Legal;

namespace Qalam.Infrastructure.Configurations.Legal;

public class LegalDocumentVersionConfiguration : IEntityTypeConfiguration<LegalDocumentVersion>
{
    public void Configure(EntityTypeBuilder<LegalDocumentVersion> builder)
    {
        builder.ToTable("LegalDocumentVersions", "legal");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).IsRequired().HasMaxLength(30);
        builder.Property(e => e.ChangeNotes).HasMaxLength(1000);

        builder.HasIndex(e => e.LegalDocumentId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.LegalDocumentId, e.MajorVersion, e.MinorVersion }).IsUnique();

        // At most one Published version per document
        builder.HasIndex(e => e.LegalDocumentId)
            .IsUnique()
            .HasFilter($"[{nameof(LegalDocumentVersion.Status)}] = '{LegalDocumentStatus.Published}'")
            .HasDatabaseName("IX_LegalDocumentVersions_OnePublished");

        builder.HasMany(e => e.Sections)
            .WithOne(s => s.LegalDocumentVersion)
            .HasForeignKey(s => s.LegalDocumentVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
