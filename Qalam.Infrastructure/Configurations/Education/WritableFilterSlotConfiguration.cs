using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class WritableFilterSlotConfiguration : IEntityTypeConfiguration<WritableFilterSlot>
{
    public void Configure(EntityTypeBuilder<WritableFilterSlot> builder)
    {
        builder.ToTable("WritableFilterSlots", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).IsRequired().HasMaxLength(80);
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(e => e.AfterStep).IsRequired().HasMaxLength(40);
        builder.Property(e => e.RequiredWhenSubjectCodeContains).HasMaxLength(40);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => new { e.DomainId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.DomainId, e.OrderIndex });

        builder.HasOne(e => e.Domain)
            .WithMany(d => d.WritableFilterSlots)
            .HasForeignKey(e => e.DomainId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Values)
            .WithOne(v => v.Slot)
            .HasForeignKey(v => v.SlotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
