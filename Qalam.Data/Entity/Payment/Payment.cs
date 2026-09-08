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
    /// Moyasar hosted invoice id retained when <see cref="ProviderTransactionId"/> is promoted
    /// to the final payment id after capture.
    /// </summary>
    [MaxLength(120)]
    public string? ProviderInvoiceId { get; set; }

    /// <summary>
    /// Gateway processing fee (e.g. Moyasar fee including their VAT), in major currency units.
    /// </summary>
    public decimal? ProviderFee { get; set; }

    /// <summary>
    /// Failure reason from the gateway (e.g. source.message) when Status is Failed.
    /// </summary>
    [MaxLength(500)]
    public string? FailureMessage { get; set; }
    
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
    public ICollection<EnrollmentPayment> EnrollmentPayments { get; set; } = new List<EnrollmentPayment>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    public ICollection<PaymentTransactionEvent> TransactionEvents { get; set; } = new List<PaymentTransactionEvent>();
}
