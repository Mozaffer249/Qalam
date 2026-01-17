using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// عنصر في عملية الدفع
/// </summary>
public class PaymentItem : AuditableEntity
{
    public int Id { get; set; }
    
    public int PaymentId { get; set; }
    
    /// <summary>
    /// نوع العنصر (تسجيل دورة، حجز جلسة، اشتراك باقة)
    /// </summary>
    public PaymentItemType ItemType { get; set; }
    
    /// <summary>
    /// معرف العنصر المرتبط (مثل CourseEnrollmentId)
    /// </summary>
    public int ReferenceId { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    /// <summary>
    /// مبلغ العنصر
    /// </summary>
    public decimal Amount { get; set; }
    
    // Navigation
    public Payment Payment { get; set; } = null!;
}
