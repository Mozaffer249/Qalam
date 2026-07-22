using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class CollegeConfiguration : IEntityTypeConfiguration<College>
{
    public void Configure(EntityTypeBuilder<College> builder)
    {
        builder.ToTable("Colleges", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.UniversityId);
        builder.HasIndex(e => new { e.UniversityId, e.Code })
               .IsUnique()
               .HasFilter("[Code] IS NOT NULL");
        builder.HasIndex(e => e.IsActive);

        builder.HasOne(e => e.University)
               .WithMany(u => u.Colleges)
               .HasForeignKey(e => e.UniversityId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Departments)
               .WithOne(d => d.College)
               .HasForeignKey(d => d.CollegeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
