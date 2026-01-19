using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionEntity = Qalam.Data.Entity.Session.Session;

namespace Qalam.Infrastructure.Configurations.Session;

public class SessionConfiguration : IEntityTypeConfiguration<SessionEntity>
{
    public void Configure(EntityTypeBuilder<SessionEntity> builder)
    {
        builder.ToTable("Sessions", "session");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.SessionRequestId);
        builder.HasIndex(e => e.TeacherId);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.Status);
        
        // Relationships
        builder.HasOne(e => e.SessionRequest)
               .WithMany(sr => sr.Sessions)
               .HasForeignKey(e => e.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Teacher)
               .WithMany()
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
        
        builder.HasOne(e => e.Student)
               .WithMany()
               .HasForeignKey(e => e.StudentId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
        
        // Collections
        builder.HasMany(e => e.ScheduledSessions)
               .WithOne(ss => ss.Session)
               .HasForeignKey(ss => ss.SessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
