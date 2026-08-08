using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common;

namespace Qalam.Infrastructure.Configurations.Common;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages", "common");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.Status, e.CreatedAt });

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Reason).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(30).IsRequired();
        builder.Property(e => e.AdminNote).HasMaxLength(2000);
    }
}
