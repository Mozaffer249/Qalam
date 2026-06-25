using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherDomainQuestionConfiguration : IEntityTypeConfiguration<TeacherDomainQuestion>
{
    public void Configure(EntityTypeBuilder<TeacherDomainQuestion> builder)
    {
        builder.ToTable("TeacherDomainQuestions", "teacher");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.DomainId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.DomainId, e.IsActive, e.SortOrder });

        builder.Property(e => e.Code).HasMaxLength(64).IsRequired();
        builder.Property(e => e.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(e => e.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(e => e.AllowedExtensionsJson).HasMaxLength(500).IsRequired();
        builder.Property(e => e.RequirementType).IsRequired();

        builder.HasOne(e => e.Domain)
            .WithMany()
            .HasForeignKey(e => e.DomainId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
