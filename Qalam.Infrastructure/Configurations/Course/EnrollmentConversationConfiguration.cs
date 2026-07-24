using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class EnrollmentConversationConfiguration : IEntityTypeConfiguration<EnrollmentConversation>
{
    public void Configure(EntityTypeBuilder<EnrollmentConversation> builder)
    {
        builder.ToTable("EnrollmentConversations", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.EnrollmentId).IsUnique();
        builder.HasIndex(e => e.TeacherId);
        builder.HasIndex(e => e.StudentUserId);
        builder.HasIndex(e => e.LastMessageAt);

        builder.HasOne(e => e.Enrollment)
               .WithMany()
               .HasForeignKey(e => e.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Teacher)
               .WithMany()
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.StudentUser)
               .WithMany()
               .HasForeignKey(e => e.StudentUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Messages)
               .WithOne(m => m.EnrollmentConversation)
               .HasForeignKey(m => m.EnrollmentConversationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
