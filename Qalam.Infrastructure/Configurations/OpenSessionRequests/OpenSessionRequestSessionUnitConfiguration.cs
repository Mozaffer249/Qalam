using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Infrastructure.Configurations.OpenSessionRequests;

public class SessionRequestSessionUnitConfiguration : IEntityTypeConfiguration<OpenSessionRequestSessionUnit>
{
    public void Configure(EntityTypeBuilder<OpenSessionRequestSessionUnit> builder)
    {
        builder.ToTable("SessionRequestSessionUnits", "sr");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.SessionRequestSessionId);
        builder.HasIndex(e => e.ContentUnitId);
        builder.HasIndex(e => e.LessonId);

        builder.HasOne(e => e.ContentUnit)
               .WithMany()
               .HasForeignKey(e => e.ContentUnitId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lesson)
               .WithMany()
               .HasForeignKey(e => e.LessonId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
