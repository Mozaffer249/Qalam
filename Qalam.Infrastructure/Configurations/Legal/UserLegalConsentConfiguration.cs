using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Legal;

namespace Qalam.Infrastructure.Configurations.Legal;

public class UserLegalConsentConfiguration : IEntityTypeConfiguration<UserLegalConsent>
{
    public void Configure(EntityTypeBuilder<UserLegalConsent> builder)
    {
        builder.ToTable("UserLegalConsents", "legal");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.Source).HasMaxLength(50);

        builder.HasIndex(e => new { e.UserId, e.LegalDocumentVersionId }).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.LegalDocumentId);
        builder.HasIndex(e => e.AcceptedAt);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LegalDocument)
            .WithMany()
            .HasForeignKey(e => e.LegalDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LegalDocumentVersion)
            .WithMany()
            .HasForeignKey(e => e.LegalDocumentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
