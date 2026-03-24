namespace Qalam.MessagingApi.Models.Entities;

public class FileUploadMessage
{
    public int TeacherId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileData { get; set; } = string.Empty; // Base64 encoded
    public int EntityId { get; set; } // TeacherDocument ID to update
    public DateTime QueuedAt { get; set; }
}
