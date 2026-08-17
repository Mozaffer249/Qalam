using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class WritableFilterValueConfiguration : IEntityTypeConfiguration<WritableFilterValue>
{
    public void Configure(EntityTypeBuilder<WritableFilterValue> builder)
    {
        builder.ToTable("WritableFilterValues", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(80);
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NormalizedText).IsRequired().HasMaxLength(200);
        builder.Property(e => e.SubjectCodeContains).HasMaxLength(40);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => new { e.SlotId, e.NormalizedText }).IsUnique();
        builder.HasIndex(e => new { e.SlotId, e.Code })
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL");
        builder.HasIndex(e => e.IsActive);
    }
}
