using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class EnrollmentConversationMessageConfiguration : IEntityTypeConfiguration<EnrollmentConversationMessage>
{
    public void Configure(EntityTypeBuilder<EnrollmentConversationMessage> builder)
    {
        builder.ToTable("EnrollmentConversationMessages", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.EnrollmentConversationId, e.SentAt });
        builder.HasIndex(e => e.SenderUserId);

        builder.Property(e => e.Content).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.MessageType).IsRequired();
        builder.Property(e => e.SentAt).IsRequired();

        builder.HasOne(e => e.SenderUser)
               .WithMany()
               .HasForeignKey(e => e.SenderUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
