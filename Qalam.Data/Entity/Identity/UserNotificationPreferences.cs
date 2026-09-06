using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qalam.Data.Entity.Identity;

/// <summary>Per-user channel preferences for push / email digests / SMS alerts.</summary>
public class UserNotificationPreferences
{
    [Key]
    public int UserId { get; set; }

    public bool PushEnabled { get; set; } = true;

    public bool EmailDigestEnabled { get; set; } = true;

    public bool SmsAlertsEnabled { get; set; } = false;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
