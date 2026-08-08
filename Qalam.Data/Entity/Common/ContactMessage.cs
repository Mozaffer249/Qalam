using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Common;

/// <summary>Public contact form submission stored for admin inbox management.</summary>
public class ContactMessage : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = null!;

    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>See <see cref="ContactReason"/>.</summary>
    [Required, MaxLength(50)]
    public string Reason { get; set; } = null!;

    [Required, MaxLength(4000)]
    public string Message { get; set; } = null!;

    /// <summary>See <see cref="ContactMessageStatus"/>.</summary>
    [Required, MaxLength(30)]
    public string Status { get; set; } = ContactMessageStatus.Open;

    [MaxLength(2000)]
    public string? AdminNote { get; set; }

    public DateTime? ClosedAt { get; set; }

    public int? ClosedByAdminUserId { get; set; }
}
