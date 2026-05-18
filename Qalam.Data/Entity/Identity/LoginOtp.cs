using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qalam.Data.Entity.Identity;

public enum LoginOtpChannel
{
    Email = 1,
    Sms = 2
}

/// <summary>
/// Pre-login OTP for teacher/student phone+email flows.
/// </summary>
public class LoginOtp
{
    [Key]
    public int Id { get; set; }

    public LoginOtpChannel Channel { get; set; }

    /// <summary>Local phone digits (without country code) — used for verify lookup.</summary>
    [Required, MaxLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [Required, MaxLength(5)]
    public string CountryCode { get; set; } = "+966";

    /// <summary>Email address OTP was delivered to.</summary>
    [MaxLength(256)]
    public string? PendingEmail { get; set; }

    [Required, MaxLength(256)]
    public string DeliveryDestination { get; set; } = null!;

    [Required, StringLength(8)]
    public string OtpCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
