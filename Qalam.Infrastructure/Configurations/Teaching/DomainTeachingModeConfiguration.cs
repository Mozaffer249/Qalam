using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Infrastructure.Configurations.Teaching;

public class DomainTeachingModeConfiguration : IEntityTypeConfiguration<DomainTeachingMode>
{
    public void Configure(EntityTypeBuilder<DomainTeachingMode> builder)
    {
        builder.ToTable("DomainTeachingModes", "teaching");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => new { e.DomainId, e.TeachingModeId }).IsUnique();
        
        builder.HasOne(e => e.Domain)
               .WithMany(d => d.DomainTeachingModes)
               .HasForeignKey(e => e.DomainId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.TeachingMode)
               .WithMany(tm => tm.DomainTeachingModes)
               .HasForeignKey(e => e.TeachingModeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

