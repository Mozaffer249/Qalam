using Microsoft.EntityFrameworkCore;

namespace Qalam.MessagingApi.Data;

public class MessagingDbContext : DbContext
{
    public MessagingDbContext(DbContextOptions<MessagingDbContext> options)
        : base(options)
    {
    }

    public DbSet<MessageLog> MessageLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MessageLog>(entity =>
        {
            entity.ToTable("MessageLogs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MessageId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Recipient)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Subject)
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasIndex(e => e.QueuedAt);
            entity.HasIndex(e => e.Status);
        });
    }
}
