using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Messaging;

/// <summary>FCM / push device token registered by a client app.</summary>
public class UserDeviceToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required, MaxLength(512)]
    public string Token { get; set; } = null!;

    /// <summary>ios | android | web</summary>
    [Required, MaxLength(20)]
    public string Platform { get; set; } = null!;

    [MaxLength(50)]
    public string? AppVersion { get; set; }

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
