using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Session;

namespace Qalam.Infrastructure.Configurations.Session;

public class ScheduledSessionConfiguration : IEntityTypeConfiguration<ScheduledSession>
{
    public void Configure(EntityTypeBuilder<ScheduledSession> builder)
    {
        builder.ToTable("ScheduledSessions", "session");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.TimeSlotId);
        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.Status);
        
        // Properties
        builder.Property(e => e.Date).HasColumnType("date");
        
        // Relationships
        builder.HasOne(e => e.Session)
               .WithMany(s => s.ScheduledSessions)
               .HasForeignKey(e => e.SessionId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.TimeSlot)
               .WithMany()
               .HasForeignKey(e => e.TimeSlotId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.TeachingMode)
               .WithMany()
               .HasForeignKey(e => e.TeachingModeId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Location)
               .WithMany()
               .HasForeignKey(e => e.LocationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
