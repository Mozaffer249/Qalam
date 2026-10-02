using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Configurations.Payment;

public class PolicyVersionConfiguration : IEntityTypeConfiguration<PolicyVersion>
{
    public void Configure(EntityTypeBuilder<PolicyVersion> builder)
    {
        builder.ToTable("PolicyVersions", "finance");
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => v.VersionNumber).IsUnique();
        builder.HasIndex(v => new { v.Status, v.EffectiveFrom });
        builder.Property(v => v.RulesJson).IsRequired();
        builder.Property(v => v.ChangeNote).HasMaxLength(500);
    }
}

public class PolicyCaseConfiguration : IEntityTypeConfiguration<PolicyCase>
{
    public void Configure(EntityTypeBuilder<PolicyCase> builder)
    {
        builder.ToTable("PolicyCases", "finance");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.EnrollmentId);
        builder.HasIndex(c => c.CourseScheduleId);
        builder.HasIndex(c => new { c.Kind, c.CreatedAt });

        // Automatic kinds are applied once per session.
        builder.HasIndex(c => new { c.Kind, c.CourseScheduleId })
            .IsUnique()
            .HasFilter($"[CourseScheduleId] IS NOT NULL AND [Kind] IN ({(int)PolicyCaseKind.TeacherNoShow}, {(int)PolicyCaseKind.StudentNoShow}, {(int)PolicyCaseKind.SessionCancel}, {(int)PolicyCaseKind.TeacherSessionCancel})");

        foreach (var p in new[] { nameof(PolicyCase.GrossValue), nameof(PolicyCase.RefundAmount), nameof(PolicyCase.FeeAmount), nameof(PolicyCase.TeacherEarningImpact), nameof(PolicyCase.PlatformRevenueImpact) })
            builder.Property(p).HasPrecision(18, 2);

        builder.Property(c => c.Currency).HasMaxLength(3).IsRequired();
        builder.Property(c => c.Reason).HasMaxLength(1000);
        builder.Property(c => c.ActorRole).HasMaxLength(30).IsRequired();

        builder.HasOne(c => c.PolicyVersion)
            .WithMany()
            .HasForeignKey(c => c.PolicyVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Enrollment)
            .WithMany()
            .HasForeignKey(c => c.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
