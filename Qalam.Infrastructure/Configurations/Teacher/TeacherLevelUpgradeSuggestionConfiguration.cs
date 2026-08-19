using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherLevelUpgradeSuggestionConfiguration : IEntityTypeConfiguration<TeacherLevelUpgradeSuggestion>
{
    public void Configure(EntityTypeBuilder<TeacherLevelUpgradeSuggestion> builder)
    {
        builder.ToTable("TeacherLevelUpgradeSuggestions", "teacher");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TeacherId, e.Status });

        builder.Property(e => e.AvgRating).HasColumnType("decimal(3,2)");
        builder.Property(e => e.AttendanceRate).HasColumnType("decimal(5,2)");
        builder.Property(e => e.ReviewNotes).HasMaxLength(500);

        builder.HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CurrentLevel)
            .WithMany()
            .HasForeignKey(e => e.CurrentLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SuggestedLevel)
            .WithMany()
            .HasForeignKey(e => e.SuggestedLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
