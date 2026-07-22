using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> builder)
    {
        builder.ToTable("Universities", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.Country).HasMaxLength(100);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.Code)
               .IsUnique()
               .HasFilter("[Code] IS NOT NULL");
        builder.HasIndex(e => e.NameEn);
        builder.HasIndex(e => e.IsActive);

        builder.HasMany(e => e.Colleges)
               .WithOne(c => c.University)
               .HasForeignKey(c => c.UniversityId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
