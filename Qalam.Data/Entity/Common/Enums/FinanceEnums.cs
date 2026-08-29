namespace Qalam.Data.Entity.Common.Enums;

public enum RefundStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3
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
    Draft = 1,
    Approved = 2,
    Paid = 3
}
