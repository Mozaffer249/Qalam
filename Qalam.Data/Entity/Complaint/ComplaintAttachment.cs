using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Complaint;

public class ComplaintAttachment : AuditableEntity
{
    public int Id { get; set; }
    public int ComplaintId { get; set; }
    public string FileUrl { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? ContentType { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }

    public Complaint Complaint { get; set; } = null!;
}
