using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class SessionLivePresenceEventConfiguration : IEntityTypeConfiguration<SessionLivePresenceEvent>
{
    public void Configure(EntityTypeBuilder<SessionLivePresenceEvent> builder)
    {
        builder.ToTable("SessionLivePresenceEvents", "course");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LiveKitEventId).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Identity).HasMaxLength(128).IsRequired();

        builder.HasIndex(e => e.LiveKitEventId).IsUnique();
        builder.HasIndex(e => new { e.CourseScheduleId, e.OccurredAt });
        builder.HasIndex(e => new { e.CourseScheduleId, e.Role, e.ParticipantId });

        builder.HasOne(e => e.CourseSchedule)
            .WithMany(s => s.LivePresenceEvents)
            .HasForeignKey(e => e.CourseScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
