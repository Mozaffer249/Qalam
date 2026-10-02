using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Messaging;

/// <summary>In-app inbox entry. Email/push are best-effort copies; this row is the record.</summary>
public class UserNotification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>Stable type code, e.g. EnrollmentCancelled, RefundCompleted.</summary>
    [Required, MaxLength(60)]
    public string Type { get; set; } = null!;

    [Required, MaxLength(200)]
    public string TitleAr { get; set; } = null!;

    [Required, MaxLength(200)]
    public string TitleEn { get; set; } = null!;

    [Required, MaxLength(1000)]
    public string BodyAr { get; set; } = null!;

    [Required, MaxLength(1000)]
    public string BodyEn { get; set; } = null!;

    /// <summary>Deep-link payload (enrollmentId, scheduleId, policyCaseId…).</summary>
    public string? DataJson { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
