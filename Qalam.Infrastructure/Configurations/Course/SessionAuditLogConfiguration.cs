using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class SessionAuditLogConfiguration : IEntityTypeConfiguration<SessionAuditLog>
{
    public void Configure(EntityTypeBuilder<SessionAuditLog> builder)
    {
        builder.ToTable("SessionAuditLogs", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseScheduleId);
        builder.HasIndex(e => e.CreatedAt);

        builder.Property(e => e.ActorRole).HasMaxLength(64);
        builder.Property(e => e.PayloadJson).HasMaxLength(8000);

        builder.HasOne(e => e.CourseSchedule)
            .WithMany(s => s.AuditLogs)
            .HasForeignKey(e => e.CourseScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
