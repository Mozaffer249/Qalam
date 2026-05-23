using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Infrastructure.Configurations.OpenSessionRequests;

public class SessionRequestInvitationConfiguration : IEntityTypeConfiguration<OpenSessionRequestInvitation>
{
    public void Configure(EntityTypeBuilder<OpenSessionRequestInvitation> builder)
    {
        builder.ToTable("SessionRequestInvitations", "sr");

        builder.HasKey(e => e.Id);

        // One invitation per (request, invitee) — no duplicate invites.
        builder.HasIndex(e => new { e.SessionRequestId, e.InvitedStudentId }).IsUnique();
        builder.HasIndex(e => e.InvitedStudentId);
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.Status).IsRequired();

        builder.HasOne(e => e.InvitedStudent)
               .WithMany()
               .HasForeignKey(e => e.InvitedStudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.InvitedByStudent)
               .WithMany()
               .HasForeignKey(e => e.InvitedByStudentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
