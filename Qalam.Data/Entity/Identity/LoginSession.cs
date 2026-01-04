using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qalam.Data.Entity.Identity
{
    public class LoginSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(255)]
        public string DeviceId { get; set; } = default!;

        [Required, MaxLength(255)]
        public string DeviceName { get; set; } = default!;

        [Required, MaxLength(50)]
        public string IpAddress { get; set; } = default!;

        [Required, MaxLength(500)]
        public string UserAgent { get; set; } = default!;

        [Required]
        public string AccessToken { get; set; } = default!;

        [Required]
        public string RefreshToken { get; set; } = default!;

        [Required]
        public DateTime LoginTime { get; set; }

        [Required]
        public DateTime LastActivityTime { get; set; }

        public DateTime? LogoutTime { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? Location { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = default!;
    }
}

