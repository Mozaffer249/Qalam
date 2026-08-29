using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Course;

public class SessionComplaintAttachment : AuditableEntity
{
    public int Id { get; set; }
    public int ComplaintId { get; set; }
    public string FileUrl { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? ContentType { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }

    public SessionComplaint Complaint { get; set; } = null!;
}
