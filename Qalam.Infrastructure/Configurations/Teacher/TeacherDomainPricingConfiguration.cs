using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherDomainPricingConfiguration : IEntityTypeConfiguration<TeacherDomainPricing>
{
    public void Configure(EntityTypeBuilder<TeacherDomainPricing> builder)
    {
        builder.ToTable("TeacherDomainPricings", "teacher");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TeacherId, e.DomainId }).IsUnique();

        builder.Property(e => e.CustomTeacherSharePct).HasColumnType("decimal(5,2)");
        builder.Property(e => e.CustomPricePerHour).HasColumnType("decimal(18,2)");
        builder.Property(e => e.ReflectCustomPriceToStudent).HasDefaultValue(false);
        builder.Property(e => e.HasCompletedInterviewSession).HasDefaultValue(false);

        builder.HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Domain)
            .WithMany()
            .HasForeignKey(e => e.DomainId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TeacherLevel)
            .WithMany()
            .HasForeignKey(e => e.TeacherLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
