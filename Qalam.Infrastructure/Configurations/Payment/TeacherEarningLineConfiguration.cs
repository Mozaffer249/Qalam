using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Configurations.Payment;

public class TeacherEarningLineConfiguration : IEntityTypeConfiguration<TeacherEarningLine>
{
    public void Configure(EntityTypeBuilder<TeacherEarningLine> builder)
    {
        builder.ToTable("TeacherEarningLines");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TeacherId);
        builder.HasIndex(e => e.EnrollmentId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.TeacherId, e.Status });
        builder.HasIndex(e => e.CourseScheduleId)
            .IsUnique()
            .HasFilter("[CourseScheduleId] IS NOT NULL");

        builder.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.Source).IsRequired();
        builder.Property(e => e.Status).IsRequired();

        builder.HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Enrollment)
            .WithMany()
            .HasForeignKey(e => e.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CourseSchedule)
            .WithMany()
            .HasForeignKey(e => e.CourseScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PayoutItem)
            .WithMany(i => i.EarningLines)
            .HasForeignKey(e => e.PayoutItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
