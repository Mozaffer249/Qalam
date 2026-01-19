using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qalam.Data.Entity.Identity;

public class PhoneConfirmationOtp
{
    [Key]
    public int Id { get; set; }
    
    [Required, MaxLength(5)]
    public string CountryCode { get; set; } = "+966";
    
    [Required, MaxLength(20)]
    public string PhoneNumber { get; set; } = null!;
    
    [Required, StringLength(4)]
    public string OtpCode { get; set; } = null!;
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    public DateTime? UsedAt { get; set; }
    
    public int? UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
