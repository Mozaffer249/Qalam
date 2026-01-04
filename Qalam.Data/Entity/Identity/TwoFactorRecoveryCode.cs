using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qalam.Data.Entity.Identity
{
    public class TwoFactorRecoveryCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(20)]
        public string Code { get; set; } = default!;

        [Required]
        public bool IsUsed { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = default!;
    }
}

