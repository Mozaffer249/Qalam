namespace Qalam.Data.Entity.Common.Enums;

public enum RefundStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3
}

/// <summary>Where refunded money goes.</summary>
public enum RefundDestination
{
    OriginalMethod = 1,
    Wallet = 2
}

public enum WalletTransactionType
{
    TopUp = 1,
    Payment = 2,
    Refund = 3,
    AdminCredit = 4,
    AdminDebit = 5,
    Reversal = 6,
    /// <summary>Debit that compensates an earlier policy credit (refund/wallet credit) on a reversed policy case.</summary>
    PolicyReversal = 7
}

public enum WalletTransactionStatus
{
    Completed = 1,
    Reversed = 2
}

public enum PolicyVersionStatus
{
    Draft = 1,
    Published = 2,
    Retired = 3
}

public enum PolicyCaseKind
{
    BeforeFirstSessionCancel = 1,
    AfterFirstSessionCancel = 2,
    SessionCancel = 3,
    TeacherNoShow = 4,
    StudentNoShow = 5,
    TechnicalIssue = 6,
    TeacherSessionCancel = 7,
    AdminException = 8,
    Reversal = 9
}

public enum PolicyCaseStatus
{
    Applied = 1,
    Failed = 2,
    Reversed = 3
}

public enum ScheduleCancellationReason
{
    StudentCancel = 1,
    TeacherCancel = 2,
    TeacherNoShow = 3,
    TechnicalIssue = 4,
    AdminCancel = 5,
    StudentReschedule = 6
}

public enum TeacherEarningSource
{
    SessionCompleted = 1,
    FreeTrialPlatform = 2
}

public enum TeacherEarningLineStatus
{
    Pending = 1,
    IncludedInPayout = 2,
    Voided = 3,
    OnHold = 4,
}

public enum PayoutBatchStatus
{
    Pending = 1,
    Approved = 2,
    Processing = 3,
    Paid = 4,
    Rejected = 5,
    Failed = 6,
    Cancelled = 7,
}

public enum TeacherBalanceAdjustmentKind
{
    Deduction = 1,
    Settlement = 2,
    Correction = 3,
}

public enum TeacherBalanceAdjustmentStatus
{
    Pending = 1,
    Applied = 2,
}

public enum TeacherDisciplinaryKind
{
    Warning = 1,
    EarningDeduction = 2,
    Fine = 3,
}
