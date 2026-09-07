using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Payments;

public static class MoyasarStatusMapper
{
    public static PaymentStatus Map(string? moyasarStatus)
    {
        if (string.IsNullOrWhiteSpace(moyasarStatus))
            return PaymentStatus.Pending;

        return moyasarStatus.Trim().ToLowerInvariant() switch
        {
            "paid" or "captured" => PaymentStatus.Succeeded,
            "failed" or "voided" => PaymentStatus.Failed,
            "refunded" => PaymentStatus.Refunded,
            "initiated" or "authorized" or "verified" => PaymentStatus.Pending,
            _ => PaymentStatus.Pending
        };
    }

    public static bool IsPaid(string? moyasarStatus)
    {
        var s = moyasarStatus?.Trim().ToLowerInvariant();
        return s is "paid" or "captured";
    }
}
