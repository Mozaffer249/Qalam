namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// حالة الدفع
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5
}

/// <summary>
/// نوع عنصر الدفع
/// </summary>
public enum PaymentItemType
{
    CourseEnrollment = 1,
    SessionBooking = 2,
    PackageSubscription = 3
}

/// <summary>Who/what produced a payment transaction audit event.</summary>
public enum PaymentTransactionEventSource
{
    Intent = 1,
    ClientConfirm = 2,
    Webhook = 3,
    Admin = 4,
    ScheduledReconciliation = 5,
    ManualReconciliation = 6,
    System = 7
}

/// <summary>Lifecycle event types for payment audit trail.</summary>
public enum PaymentTransactionEventType
{
    IntentCreated = 1,
    IntentReused = 2,
    IntentCancelled = 3,
    CheckoutCreated = 4,
    CheckoutFailed = 5,
    WebhookReceived = 10,
    WebhookAuthRejected = 11,
    WebhookUnmatched = 12,
    WebhookProviderMismatch = 13,
    WebhookDuplicate = 14,
    WebhookProcessed = 15,
    ConfirmRequested = 20,
    ConfirmRemoteVerified = 21,
    ConfirmAmountMismatch = 22,
    ConfirmSucceeded = 23,
    ConfirmFailed = 24,
    ConfirmIdempotentReplay = 25,
    EnrollmentActivated = 30,
    ScheduleConflict = 31,
    StatusChanged = 40,
    RefundRequested = 50,
    RefundSucceeded = 51,
    RefundFailed = 52,
    ReconciliationMatch = 60,
    ReconciliationRepair = 61,
    ReconciliationMismatch = 62,
    ReconciliationUnresolvedRemote = 63,
    ReconciliationMissingRemote = 64
}

/// <summary>Outcome of processing a payment transaction event.</summary>
public enum PaymentTransactionEventResult
{
    Success = 1,
    Ignored = 2,
    Failed = 3,
    Unauthorized = 4,
    NotFound = 5,
    Mismatch = 6,
    Pending = 7
}

/// <summary>Lifecycle of a Moyasar reconciliation run.</summary>
public enum PaymentReconciliationRunStatus
{
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    PartiallySucceeded = 4
}

/// <summary>How a reconciliation run was started.</summary>
public enum PaymentReconciliationRunSource
{
    Manual = 1,
    Scheduled = 2
}
