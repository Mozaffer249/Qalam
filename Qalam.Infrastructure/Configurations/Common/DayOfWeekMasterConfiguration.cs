using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common;

namespace Qalam.Infrastructure.Configurations.Common;

public class DayOfWeekMasterConfiguration : IEntityTypeConfiguration<DayOfWeekMaster>
{
    public void Configure(EntityTypeBuilder<DayOfWeekMaster> builder)
    {
        builder.ToTable("DaysOfWeek");
        
        builder.HasKey(e => e.Id);
        
        // Disable identity - we want to use specific IDs (1-7 for Sunday-Saturday)
        builder.Property(e => e.Id)
            .ValueGeneratedNever();
        
        builder.HasIndex(e => e.OrderIndex);
        builder.HasIndex(e => e.IsActive);
        
        builder.Property(e => e.NameAr)
            .IsRequired()
            .HasMaxLength(30);
            
        builder.Property(e => e.NameEn)
            .IsRequired()
            .HasMaxLength(30);
    }
}
