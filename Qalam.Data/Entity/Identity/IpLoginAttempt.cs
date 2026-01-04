using System;
using System.ComponentModel.DataAnnotations;

namespace Qalam.Data.Entity.Identity
{
    public class IpLoginAttempt
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string IpAddress { get; set; } = default!;

        [Required]
        public DateTime AttemptTime { get; set; } = DateTime.UtcNow;

        [Required]
        public bool WasSuccessful { get; set; }

        public int? UserId { get; set; }  // Null for unknown users

        [MaxLength(256)]
        public string? UserName { get; set; }  // For audit purposes
    }
}

