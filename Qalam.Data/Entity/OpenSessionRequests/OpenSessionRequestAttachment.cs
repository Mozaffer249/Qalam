using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// ملف مرفق برفعه الطالب مع طلب الجلسات (PDF، صور، الخ).
/// التخزين على Alibaba OSS عبر IFileStorageService.
/// </summary>
public class OpenSessionRequestAttachment : AuditableEntity
{
    public int Id { get; set; }

    public int SessionRequestId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(1024)]
    public string StorageKey { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string? PublicUrl { get; set; }

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// حجم الملف بالبايت.
    /// </summary>
    public long FileSizeBytes { get; set; }

    // Navigation Properties
    public OpenSessionRequest OpenSessionRequest { get; set; } = null!;
}
