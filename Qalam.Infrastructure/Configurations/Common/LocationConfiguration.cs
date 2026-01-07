using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common;

namespace Qalam.Infrastructure.Configurations.Common;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations", "common");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.ParentLocationId);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.IsActive);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Latitude).HasPrecision(9, 6);
        builder.Property(e => e.Longitude).HasPrecision(9, 6);
        
        builder.HasOne(e => e.ParentLocation)
               .WithMany(l => l.ChildLocations)
               .HasForeignKey(e => e.ParentLocationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

