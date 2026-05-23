using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// رسالة ضمن محادثة عرض معلم. تشمل رسائل نصية يدوية ورسائل نظام تلقائية (تم تقديم العرض، تم التحديث، الخ).
/// </summary>
public class OfferMessage : AuditableEntity
{
    public int Id { get; set; }

    public int OfferConversationId { get; set; }

    /// <summary>
    /// مرسل الرسالة. لرسائل النظام تكون فارغة.
    /// </summary>
    public int? SenderUserId { get; set; }

    public OfferMessageType MessageType { get; set; } = OfferMessageType.Text;

    [Required, MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public OfferConversation OfferConversation { get; set; } = null!;
    public Identity.User? SenderUser { get; set; }
}
