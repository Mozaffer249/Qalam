namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// حالة طلب الجلسات المفتوح (السيناريو الثاني)
/// </summary>
public enum OpenSessionRequestStatus
{
    Draft = 1,
    PendingInvitations = 2,
    Active = 3,
    ReceivingOffers = 4,
    OfferAccepted = 5,
    PaymentPending = 6,
    Paid = 7,
    Scheduled = 8,
    InProgress = 9,
    Completed = 10,
    Cancelled = 11,
    Expired = 12
}

/// <summary>
/// حالة عرض المعلم على طلب الجلسات
/// </summary>
public enum OpenSessionOfferStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    AutoRejected = 4,
    Withdrawn = 5,
    Expired = 6
}

/// <summary>
/// حالة دعوة طالب للانضمام لطلب جلسات
/// </summary>
public enum OpenSessionRequestInvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4
}

/// <summary>
/// نوع مجموعة العرض: مفتوحة (يقبل طلاب إضافيين) أو دعوة فقط
/// </summary>
public enum OfferGroupType
{
    OpenGroup = 1,
    InviteOnly = 2
}

/// <summary>
/// حالة استهداف المعلم في طلب جلسات (للإشعارات)
/// </summary>
public enum OpenSessionRequestTargetStatus
{
    Notified = 1,
    Viewed = 2,
    OfferSubmitted = 3,
    Skipped = 4
}

/// <summary>
/// نوع رسالة محادثة العرض
/// </summary>
public enum OfferMessageType
{
    Text = 1,
    System = 2,
    OfferUpdate = 3
}
