using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Session;

/// <summary>
/// عرض معلم على طلب جلسة
/// </summary>
public class SessionRequestOffer : AuditableEntity
{
    public int Id { get; set; }
    
    public int SessionRequestId { get; set; }
    public int TeacherId { get; set; }
    
    /// <summary>
    /// السعر المقترح
    /// </summary>
    public decimal ProposedPrice { get; set; }
    
    /// <summary>
    /// الجدول المقترح (نص حر)
    /// </summary>
    [MaxLength(800)]
    public string? ProposedSchedule { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    
    // Navigation Properties
    public SessionRequest SessionRequest { get; set; } = null!;
    public Teacher.Teacher Teacher { get; set; } = null!;
}
