using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class EnrollmentSelectedSessionSlotUnitConfiguration
    : IEntityTypeConfiguration<EnrollmentSelectedSessionSlotUnit>
{
    public void Configure(EntityTypeBuilder<EnrollmentSelectedSessionSlotUnit> builder)
    {
        builder.ToTable("EnrollmentSelectedSessionSlotUnits", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.EnrollmentSelectedSessionSlotId);
        builder.HasIndex(e => e.ContentUnitId);
        builder.HasIndex(e => e.LessonId);

        builder.HasOne(e => e.SessionSlot)
               .WithMany(s => s.Units)
               .HasForeignKey(e => e.EnrollmentSelectedSessionSlotId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ContentUnit)
               .WithMany()
               .HasForeignKey(e => e.ContentUnitId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lesson)
               .WithMany()
               .HasForeignKey(e => e.LessonId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
