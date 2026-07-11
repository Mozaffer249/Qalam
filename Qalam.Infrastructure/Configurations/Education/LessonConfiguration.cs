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
        builder.HasIndex(e => new { e.UnitId, e.NameEn })
            .IsUnique()
            .HasFilter("[QuranContentTypeId] IS NULL AND [QuranLevelId] IS NULL");
        builder.HasIndex(e => new { e.UnitId, e.QuranContentTypeId, e.QuranLevelId, e.NameEn })
            .IsUnique()
            .HasFilter("[QuranContentTypeId] IS NOT NULL AND [QuranLevelId] IS NOT NULL");

        builder.HasOne(e => e.Unit)
               .WithMany(u => u.Lessons)
               .HasForeignKey(e => e.UnitId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.QuranContentType)
               .WithMany()
               .HasForeignKey(e => e.QuranContentTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.QuranLevel)
               .WithMany()
               .HasForeignKey(e => e.QuranLevelId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

