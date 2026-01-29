using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Infrastructure.Configurations.Teaching;

public class EducationRuleConfiguration : IEntityTypeConfiguration<EducationRule>
{
    public void Configure(EntityTypeBuilder<EducationRule> builder)
    {
        builder.ToTable("EducationRules", "teaching");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.DomainId).IsUnique();

        builder.Property(e => e.MinSessions).HasDefaultValue(1);
        builder.Property(e => e.MaxSessions).HasDefaultValue(100);
        builder.Property(e => e.DefaultSessionDurationMinutes).HasDefaultValue(60);
        builder.Property(e => e.AllowExtension).HasDefaultValue(true);
        builder.Property(e => e.AllowFlexibleCourses).HasDefaultValue(true);

        builder.Property(e => e.NotesAr).HasMaxLength(500);
        builder.Property(e => e.NotesEn).HasMaxLength(500);

        // Check constraints for sanity
        builder.ToTable(t => t.HasCheckConstraint("CK_EducationRules_SessionsRange", "[MinSessions] <= [MaxSessions]"));
        builder.ToTable(t => t.HasCheckConstraint("CK_EducationRules_GroupRange",
            "([MinGroupSize] IS NULL AND [MaxGroupSize] IS NULL) OR ([MinGroupSize] <= [MaxGroupSize])"));

        builder.HasOne(e => e.Domain)
               .WithOne(d => d.EducationRule)
               .HasForeignKey<EducationRule>(e => e.DomainId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

