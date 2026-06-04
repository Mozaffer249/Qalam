using Qalam.Data.Commons;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// محادثة بين معلم محدد وصاحب طلب الجلسات (الطالب/ولي الأمر). تُنشأ عند أول تفاعل،
/// قد يكون ذلك قبل تقديم العرض (شات "طلب توضيح" التمهيدي) أو معه. المفتاح الطبيعي
/// هو (SessionRequestId, TeacherId)؛ SessionOfferId مرجع اختياري يُملأ عند وجود عرض.
/// </summary>
public class OfferConversation : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>الطلب المرتبط بالمحادثة. مع TeacherId يشكّل مفتاحاً فريداً.</summary>
    public int SessionRequestId { get; set; }

    /// <summary>المعلم المشارك في المحادثة. مع SessionRequestId يشكّل مفتاحاً فريداً.</summary>
    public int TeacherId { get; set; }

    /// <summary>
    /// عرض المعلم الحالي على هذا الطلب — يُملأ عند تقديم العرض ويُستخدم لعرض ملخص العرض في
    /// رأس شاشة الشات. قد يكون فارغاً لو الشات تمهيدي (قبل تقديم العرض).
    /// </summary>
    public int? SessionOfferId { get; set; }

    /// <summary>آخر وقت قرأ فيه الطالب الرسائل (للعدّاد غير المقروء).</summary>
    public DateTime? StudentLastReadAt { get; set; }

    /// <summary>آخر وقت قرأ فيه المعلم الرسائل.</summary>
    public DateTime? TeacherLastReadAt { get; set; }

    /// <summary>تاريخ آخر رسالة (للترتيب السريع).</summary>
    public DateTime? LastMessageAt { get; set; }

    // Navigation Properties
    public OpenSessionRequest OpenSessionRequest { get; set; } = null!;
    public Teacher.Teacher Teacher { get; set; } = null!;
    public OpenSessionOffer? OpenSessionOffer { get; set; }
    public ICollection<OfferMessage> Messages { get; set; } = new List<OfferMessage>();
}
