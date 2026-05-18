using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Identity;

namespace Qalam.Infrastructure.Configurations.Identity;

public class LoginOtpConfiguration : IEntityTypeConfiguration<LoginOtp>
{
    public void Configure(EntityTypeBuilder<LoginOtp> builder)
    {
        builder.ToTable("LoginOtps", "identity");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.PhoneNumber, e.IsUsed, e.ExpiresAt });
        builder.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(e => e.CountryCode).IsRequired().HasMaxLength(5);
        builder.Property(e => e.PendingEmail).HasMaxLength(256);
        builder.Property(e => e.DeliveryDestination).IsRequired().HasMaxLength(256);
        builder.Property(e => e.OtpCode).IsRequired().HasMaxLength(8);
    }
}
