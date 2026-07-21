using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common;

namespace Qalam.Infrastructure.Configurations.Common;

public class NationalityConfiguration : IEntityTypeConfiguration<Nationality>
{
    public void Configure(EntityTypeBuilder<Nationality> builder)
    {
        builder.ToTable("Nationalities", "common");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => new { e.IsActive, e.SortOrder });

        builder.Property(e => e.Code).HasMaxLength(2).IsRequired();
        builder.Property(e => e.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(e => e.NameEn).HasMaxLength(200).IsRequired();
        // Flag emoji is 2 regional indicators (up to 8 UTF-16 code units); keep headroom.
        builder.Property(e => e.FlagEmoji).HasMaxLength(32);
    }
}
