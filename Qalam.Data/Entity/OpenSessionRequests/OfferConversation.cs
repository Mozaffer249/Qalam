using Qalam.Data.Commons;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// محادثة بين معلم محدد وصاحب طلب الجلسات (الطالب/ولي الأمر).
/// Targeted requests: one thread per (SessionRequestId, TeacherId), IsOfferScoped=false;
/// SessionOfferId is an optional pointer to the current offer (pre-offer chat allowed).
/// Broadcast requests: one thread per offer, IsOfferScoped=true; SessionOfferId required.
/// </summary>
public class OfferConversation : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>الطلب المرتبط بالمحادثة.</summary>
    public int SessionRequestId { get; set; }

    /// <summary>المعلم المشارك في المحادثة.</summary>
    public int TeacherId { get; set; }

    /// <summary>
    /// When false (targeted): unique with TeacherId on the request; SessionOfferId is a mutable pointer.
    /// When true (broadcast): unique on SessionOfferId; one conversation per offer.
    /// </summary>
    public bool IsOfferScoped { get; set; }

    /// <summary>
    /// Targeted: optional pointer to the current offer (null = preliminary chat).
    /// Broadcast: required identity of this conversation's offer.
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
