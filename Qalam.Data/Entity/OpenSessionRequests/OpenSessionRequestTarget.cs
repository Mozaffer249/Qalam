using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// المعلمون المؤهلون الذين تم إبلاغهم بطلب جلسات (لقطة من خوارزمية المطابقة عند النشر).
/// كل صف يمثل معلماً تم استهدافه + حالة استجابته للإشعار.
/// </summary>
public class OpenSessionRequestTarget : AuditableEntity
{
    public int Id { get; set; }

    public int SessionRequestId { get; set; }

    public int TeacherId { get; set; }

    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;

    public DateTime? NotifiedAt { get; set; }

    public DateTime? ViewedAt { get; set; }

    public OpenSessionRequestTargetStatus Status { get; set; } = OpenSessionRequestTargetStatus.Notified;

    // Navigation Properties
    public OpenSessionRequest OpenSessionRequest { get; set; } = null!;
    public Teacher.Teacher Teacher { get; set; } = null!;
}
