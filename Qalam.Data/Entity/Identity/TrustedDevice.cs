using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qalam.Data.Entity.Identity
{
    public class TrustedDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(255)]
        public string DeviceId { get; set; } = default!;

        [Required, MaxLength(255)]
        public string DeviceName { get; set; } = default!;

        [Required, MaxLength(500)]
        public string DeviceFingerprint { get; set; } = default!;

        [Required]
        public DateTime TrustedAt { get; set; }

        public DateTime? LastUsedAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = default!;
    }
}

