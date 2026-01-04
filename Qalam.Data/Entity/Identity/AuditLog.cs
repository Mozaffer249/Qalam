using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qalam.Data.Entity.Identity
{
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        public int? UserId { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = default!;

        [MaxLength(100)]
        public string? EntityType { get; set; }

        [MaxLength(100)]
        public string? EntityId { get; set; }

        [Required, MaxLength(50)]
        public string IpAddress { get; set; } = default!;

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public string? Details { get; set; }

        [Required]
        public bool Success { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}

