using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherSubjectUnitConfiguration : IEntityTypeConfiguration<TeacherSubjectUnit>
{
    public void Configure(EntityTypeBuilder<TeacherSubjectUnit> builder)
    {
        builder.ToTable("TeacherSubjectUnits", "teacher");
        
        builder.HasKey(e => e.Id);
        
        // Indexes for filtering
        builder.HasIndex(e => e.TeacherSubjectId);
        builder.HasIndex(e => e.UnitId);
        builder.HasIndex(e => e.QuranContentTypeId);
        builder.HasIndex(e => e.QuranLevelId);
        
        // Composite unique index (same unit can have multiple records with different ContentType/Level)
        builder.HasIndex(e => new { e.TeacherSubjectId, e.UnitId, e.QuranContentTypeId, e.QuranLevelId })
               .IsUnique()
               .HasFilter(null); // Include nulls in unique constraint
        
        // Composite index for teacher search queries
        builder.HasIndex(e => new { e.UnitId, e.QuranContentTypeId, e.QuranLevelId });
        
        // Relationships
        builder.HasOne(e => e.TeacherSubject)
               .WithMany(ts => ts.TeacherSubjectUnits)
               .HasForeignKey(e => e.TeacherSubjectId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Unit)
               .WithMany()
               .HasForeignKey(e => e.UnitId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.QuranContentType)
               .WithMany()
               .HasForeignKey(e => e.QuranContentTypeId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.QuranLevel)
               .WithMany()
               .HasForeignKey(e => e.QuranLevelId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
