namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// حالة الدورة
/// </summary>
public enum CourseStatus
{
    Draft = 1,
    Published = 2,
    Paused = 3
}

/// <summary>
/// حالة التسجيل
/// </summary>
public enum EnrollmentStatus
{
    PendingPayment = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>
/// حالة تأكيد عضو المجموعة
/// </summary>
public enum GroupMemberConfirmationStatus
{
    Pending = 1,
    Confirmed = 2,
    Rejected = 3
}
