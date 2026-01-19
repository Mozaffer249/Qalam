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
