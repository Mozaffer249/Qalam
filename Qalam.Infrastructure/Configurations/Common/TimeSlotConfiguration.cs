using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common;

namespace Qalam.Infrastructure.Configurations.Common;

public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.ToTable("TimeSlots", "common");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => new { e.StartTime, e.EndTime });
        builder.HasIndex(e => e.IsActive);
        
        builder.Property(e => e.LabelAr).HasMaxLength(50);
        builder.Property(e => e.LabelEn).HasMaxLength(50);
    }
}

