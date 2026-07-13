namespace Qalam.Data.Entity.Messaging;

/// <summary>
/// RabbitMQ payload published by Qalam.Api when a teacher uploads a content library file.
/// MessagingApi consumes it, uploads to OSS, then updates UploadStatus on teacher.TeacherContentItems.
/// </summary>
public class TeacherContentFileUploadMessage
{
    public int TeacherId { get; set; }
    public int ContentItemId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string FileData { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
}
