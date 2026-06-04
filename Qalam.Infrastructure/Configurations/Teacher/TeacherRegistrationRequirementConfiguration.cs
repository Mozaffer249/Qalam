using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherRegistrationRequirementConfiguration : IEntityTypeConfiguration<TeacherRegistrationRequirement>
{
    public void Configure(EntityTypeBuilder<TeacherRegistrationRequirement> builder)
    {
        builder.ToTable("TeacherRegistrationRequirements", "teacher");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => new { e.IsActive, e.SortOrder });

        builder.Property(e => e.Code).HasMaxLength(64).IsRequired();
        builder.Property(e => e.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(e => e.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(e => e.AllowedExtensionsJson).HasMaxLength(500).IsRequired();
        builder.Property(e => e.RequirementType).IsRequired();
    }
}
