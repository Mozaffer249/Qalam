namespace Qalam.MessagingApi.Models.Entities;

public class TeacherDocUploadMessage
{
    public int TeacherId { get; set; }
    public string DocumentType { get; set; } = string.Empty; // "identity", "certificates", "reupload"
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileData { get; set; } = string.Empty; // Base64 encoded
    public int DocumentId { get; set; } // TeacherDocuments.Id
    public DateTime QueuedAt { get; set; }
}
