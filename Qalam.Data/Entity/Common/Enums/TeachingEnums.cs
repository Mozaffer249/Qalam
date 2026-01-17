namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// حالة المعلم
/// </summary>
public enum TeacherStatus
{
    Pending = 1,
    Active = 2,
    Blocked = 3
}

/// <summary>
/// نوع وثيقة المعلم
/// </summary>
public enum TeacherDocumentType
{
    Id = 1,
    Certificate = 2,
    Other = 3
}

/// <summary>
/// نوع استثناء التوفر
/// </summary>
public enum AvailabilityExceptionType
{
    Blocked = 1,
    Extra = 2
}

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
/// حالة الطلب
/// </summary>
public enum RequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
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
/// حالة الجدول
/// </summary>
public enum ScheduleStatus
{
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3,
    Rescheduled = 4
}

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

/// <summary>
/// الجنس
/// </summary>
public enum Gender
{
    Male = 1,
    Female = 2
}

/// <summary>
/// علاقة ولي الأمر
/// </summary>
public enum GuardianRelation
{
    Father = 1,
    Mother = 2,
    Brother = 3,
    Sister = 4,
    Uncle = 5,
    Aunt = 6,
    Grandfather = 7,
    Grandmother = 8,
    Other = 99
}
