using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// عرض معلم على طلب جلسات (السعر، الموعد المقترح، صلاحية العرض).
/// قاعدة: عرض واحد فعّال لكل معلم لكل طلب (مفروض بفهرس فريد + مدقّق).
/// </summary>
public class OpenSessionOffer : AuditableEntity
{
    public int Id { get; set; }

    public int SessionRequestId { get; set; }

    public int TeacherId { get; set; }

    /// <summary>
    /// السعر الإجمالي بالعملة الأساسية (SAR افتراضاً).
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// نوع المجموعة (للعروض على طلبات جماعية).
    /// </summary>
    public OfferGroupType? GroupType { get; set; }

    /// <summary>
    /// رقم النسخة. يزداد بواحد عند كل تعديل من المعلم.
    /// </summary>
    public int Version { get; set; } = 1;

    public OpenSessionOfferStatus Status { get; set; } = OpenSessionOfferStatus.Pending;

    /// <summary>
    /// تاريخ انتهاء صلاحية العرض. الافتراضي: 48 ساعة من الإنشاء.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public DateTime? ExpiredAt { get; set; }

    [MaxLength(1000)]
    public string? TeacherNotes { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    // Navigation Properties
    public OpenSessionRequest OpenSessionRequest { get; set; } = null!;
    public Teacher.Teacher Teacher { get; set; } = null!;

    // Note: the teacher does NOT propose a schedule. The student-requested timing on
    // OpenSessionRequestSession is what the teacher implicitly accepts when offering.
    public OfferConversation? Conversation { get; set; }
}
