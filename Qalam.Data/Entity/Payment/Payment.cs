using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// عملية دفع
/// </summary>
public class Payment : AuditableEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// معرف المستخدم الدافع
    /// </summary>
    public int PayerUserId { get; set; }
    
    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";
    
    [Required, MaxLength(40)]
    public string PaymentProvider { get; set; } = null!;
    
    [MaxLength(120)]
    public string? ProviderTransactionId { get; set; }
    
    /// <summary>
    /// المبلغ قبل الضريبة
    /// </summary>
    public decimal Subtotal { get; set; }
    
    /// <summary>
    /// مبلغ الضريبة
    /// </summary>
    public decimal VatAmount { get; set; }
    
    /// <summary>
    /// مبلغ الخصم
    /// </summary>
    public decimal DiscountAmount { get; set; }
    
    /// <summary>
    /// المبلغ الإجمالي
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    [MaxLength(50)]
    public string? InvoiceNumber { get; set; }
    
    [MaxLength(600)]
    public string? ReceiptUrl { get; set; }
    
    [MaxLength(600)]
    public string? ReceiptPath { get; set; }
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    // Navigation Properties
    public User PayerUser { get; set; } = null!;
    
    public ICollection<PaymentItem> PaymentItems { get; set; } = new List<PaymentItem>();
    public ICollection<CourseEnrollmentPayment> CourseEnrollmentPayments { get; set; } = new List<CourseEnrollmentPayment>();
}
