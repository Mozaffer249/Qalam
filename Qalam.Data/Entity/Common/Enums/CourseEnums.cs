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
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// نوع عضو المجموعة (طالب المالك أو مدعو)
/// </summary>
public enum GroupMemberType
{
    Own = 1,
    Invited = 2
}

/// <summary>
/// شكل التسجيل: فردي أو جماعي
/// </summary>
public enum EnrollmentKind
{
    Individual = 1,
    Group = 2
}

/// <summary>
/// مصدر التسجيل: من طلب دورة (السيناريو الأول) أو من طلب جلسات مفتوح (السيناريو الثاني)
/// </summary>
public enum EnrollmentSource
{
    CourseRequest = 1,
    SessionRequest = 2
}
