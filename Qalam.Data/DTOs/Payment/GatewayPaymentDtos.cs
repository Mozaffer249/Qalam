using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Payment;

/// <summary>
/// Normalized view of a payment as returned by a gateway (e.g. Moyasar).
/// Amounts are in the smallest currency unit (halalas for SAR).
/// </summary>
public class GatewayPaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public PaymentStatus MappedStatus { get; set; }
    public int AmountHalalas { get; set; }
    public string Currency { get; set; } = "SAR";
    public int? FeeHalalas { get; set; }
    public string? SourceCompany { get; set; }
    public string? SourceNumber { get; set; }
    public string? Message { get; set; }
    public string? TransactionUrl { get; set; }

    /// <summary>Parent Moyasar invoice id when this payment was created via hosted invoice.</summary>
    public string? InvoiceId { get; set; }
}

public class GatewayRefundDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AmountHalalas { get; set; }
    public string Currency { get; set; } = "SAR";
    public string? Message { get; set; }
}
