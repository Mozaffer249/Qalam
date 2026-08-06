using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Messaging;

namespace Qalam.Infrastructure.Configurations.Messaging;

public class EmailSuppressionConfiguration : IEntityTypeConfiguration<EmailSuppression>
{
    public void Configure(EntityTypeBuilder<EmailSuppression> builder)
    {
        builder.ToTable("EmailSuppressions", "messaging");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Diagnostic)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.HasIndex(e => e.LastBounceAt);
    }
}
