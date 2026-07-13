using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherContentItemConfiguration : IEntityTypeConfiguration<TeacherContentItem>
{
    public void Configure(EntityTypeBuilder<TeacherContentItem> builder)
    {
        builder.ToTable("TeacherContentItems", "teacher");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(300);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.StorageKey).HasMaxLength(1024);
        builder.Property(e => e.PublicUrl).HasMaxLength(1024);
        builder.Property(e => e.TagsJson).HasMaxLength(2000).HasDefaultValue("[]");

        builder.HasIndex(e => new { e.TeacherId, e.FolderId });

        builder.HasOne(e => e.Folder)
            .WithMany(e => e.Items)
            .HasForeignKey(e => e.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
