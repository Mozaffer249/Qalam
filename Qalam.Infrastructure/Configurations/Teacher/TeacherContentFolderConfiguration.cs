using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherContentFolderConfiguration : IEntityTypeConfiguration<TeacherContentFolder>
{
    public void Configure(EntityTypeBuilder<TeacherContentFolder> builder)
    {
        builder.ToTable("TeacherContentFolders", "teacher");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(e => new { e.TeacherId, e.ParentFolderId, e.Name }).IsUnique();

        builder.HasOne(e => e.ParentFolder)
            .WithMany(e => e.ChildFolders)
            .HasForeignKey(e => e.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
