using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qalam.Data.Entity.Identity
{
    public class SecurityEvent
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public SecurityEventType EventType { get; set; }

        [Required, MaxLength(50)]
        public string IpAddress { get; set; } = default!;

        [Required]
        public string Details { get; set; } = default!;

        [Required]
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool WasNotified { get; set; } = false;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = default!;
    }

    public enum SecurityEventType
    {
        LoginFromNewDevice,
        LoginFromNewLocation,
        PasswordChanged,
        EmailChanged,
        TwoFactorEnabled,
        TwoFactorDisabled,
        FailedLoginAttempt,
        AccountLocked,
        SuspiciousActivity
    }
}

