using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.UnitId);
        builder.HasIndex(e => new { e.UnitId, e.NameEn }).IsUnique();

        builder.HasOne(e => e.Unit)
               .WithMany(u => u.Lessons)
               .HasForeignKey(e => e.UnitId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

