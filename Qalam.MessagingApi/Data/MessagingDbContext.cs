using Microsoft.EntityFrameworkCore;
using Qalam.MessagingApi.Models.Entities;

namespace Qalam.MessagingApi.Data;

public class MessagingDbContext : DbContext
{
    public MessagingDbContext(DbContextOptions<MessagingDbContext> options)
        : base(options)
    {
    }

    public DbSet<MessageLog> MessageLogs { get; set; } = null!;
    public DbSet<EmailSuppression> EmailSuppressions { get; set; } = null!;
    public DbSet<LrsStatementLog> LrsStatementLogs { get; set; } = null!;
    public DbSet<SystemSettingRow> SystemSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MessageLog>(entity =>
        {
            // Same table as Qalam.Infrastructure migration (schema messaging) — shared SQL database.
            entity.ToTable("MessageLogs", "messaging");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MessageId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Recipient)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Subject)
                .HasMaxLength(500);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            // Stored as int — matches ApplicationDBContext / AddMessagingAndFixCascade migration.
            entity.Property(e => e.Status);
            entity.Property(e => e.Type);

            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasIndex(e => e.QueuedAt);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<LrsStatementLog>(entity =>
        {
            entity.ToTable("LrsStatementLogs", "lrs");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<SystemSettingRow>(entity =>
        {
            entity.ToTable("SystemSettings", "common");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<EmailSuppression>(entity =>
        {
            entity.ToTable("EmailSuppressions", "messaging");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Diagnostic).HasMaxLength(2000);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.LastBounceAt);
        });
    }
}
