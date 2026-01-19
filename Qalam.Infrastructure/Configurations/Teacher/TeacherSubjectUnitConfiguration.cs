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
        
        // Indexes
        builder.HasIndex(e => e.TeacherSubjectId);
        builder.HasIndex(e => e.UnitId);
        builder.HasIndex(e => new { e.TeacherSubjectId, e.UnitId })
               .IsUnique();
        
        // Relationships
        builder.HasOne(e => e.TeacherSubject)
               .WithMany(ts => ts.TeacherSubjectUnits)
               .HasForeignKey(e => e.TeacherSubjectId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Unit)
               .WithMany()
               .HasForeignKey(e => e.UnitId)
               .OnDelete(DeleteBehavior.Restrict); // Changed to Restrict to avoid cascade path conflict
    }
}
