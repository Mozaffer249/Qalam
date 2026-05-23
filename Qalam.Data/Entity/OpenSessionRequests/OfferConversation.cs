using Qalam.Data.Commons;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// محادثة مرتبطة بعرض معلم واحد. يُنشأ تلقائياً عند أول رسالة.
/// المشاركون الوحيدون: مالك طلب الجلسات (الطالب) ومقدّم العرض (المعلم).
/// </summary>
public class OfferConversation : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// عرض المعلم المرتبط بالمحادثة (فهرس فريد).
    /// </summary>
    public int SessionOfferId { get; set; }

    /// <summary>
    /// آخر وقت قرأ فيه الطالب الرسائل (للعدّاد غير المقروء).
    /// </summary>
    public DateTime? StudentLastReadAt { get; set; }

    /// <summary>
    /// آخر وقت قرأ فيه المعلم الرسائل.
    /// </summary>
    public DateTime? TeacherLastReadAt { get; set; }

    /// <summary>
    /// تاريخ آخر رسالة (للترتيب السريع).
    /// </summary>
    public DateTime? LastMessageAt { get; set; }

    // Navigation Properties
    public OpenSessionOffer OpenSessionOffer { get; set; } = null!;
    public ICollection<OfferMessage> Messages { get; set; } = new List<OfferMessage>();
}
