namespace Qalam.Data.DTOs.Wallet;

public class WalletSummaryDto
{
    public int WalletId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "SAR";
    public decimal TotalAdded { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal MinTopUp { get; set; }
    public decimal MaxTopUp { get; set; }
}

public class WalletTransactionDto
{
    public int Id { get; set; }

    /// <summary>TopUp, Payment, Refund, AdminCredit, AdminDebit, Reversal.</summary>
    public string Type { get; set; } = "";

    /// <summary>Signed: positive = money in, negative = money out.</summary>
    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    /// <summary>Completed or Reversed.</summary>
    public string Status { get; set; } = "Completed";
    public int? PolicyCaseId { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime CreatedAt { get; set; }

    /// <summary>Course title or session-request subject when linked to an enrollment.</summary>
    public string? Title { get; set; }

    public string? SessionLabel { get; set; }
    public string? Description { get; set; }
    public string? ReasonCode { get; set; }
    public int? PaymentId { get; set; }
    public int? RefundId { get; set; }
    public int? EnrollmentId { get; set; }
    public int? CourseScheduleId { get; set; }
    public int? ComplaintId { get; set; }
}

public class CreateWalletTopUpDto
{
    public decimal Amount { get; set; }

    /// <summary>Optional app/web return URL (same rules as enrollment payment intents).</summary>
    public string? AppReturnUrl { get; set; }
}

public class PayWithWalletDto
{
    public int ParticipantId { get; set; }
}

public class AdminWalletAdjustmentDto
{
    /// <summary>Signed: positive credits the wallet, negative debits it.</summary>
    public decimal Amount { get; set; }

    public string Reason { get; set; } = "";
}

public class AdminStudentWalletDto
{
    public int PayerUserId { get; set; }
    public string? PayerName { get; set; }

    /// <summary>True when the wallet belongs to the student's guardian.</summary>
    public bool IsGuardianWallet { get; set; }

    public WalletSummaryDto Summary { get; set; } = new();
}
