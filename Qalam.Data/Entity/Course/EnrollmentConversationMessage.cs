using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// رسالة ضمن محادثة تسجيل (نص يدوي أو رسالة نظام).
/// </summary>
public class EnrollmentConversationMessage : AuditableEntity
{
    public int Id { get; set; }

    public int EnrollmentConversationId { get; set; }

    /// <summary>مرسل الرسالة. فارغ لرسائل النظام.</summary>
    public int? SenderUserId { get; set; }

    public EnrollmentMessageType MessageType { get; set; } = EnrollmentMessageType.Text;

    [Required, MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public EnrollmentConversation EnrollmentConversation { get; set; } = null!;
    public Identity.User? SenderUser { get; set; }
}
