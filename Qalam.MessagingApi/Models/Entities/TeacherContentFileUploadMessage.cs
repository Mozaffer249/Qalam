namespace Qalam.MessagingApi.Models.Entities;

/// <summary>
/// Consumer-side copy of the TeacherContentFileUpload payload published by Qalam.Api.
/// Keep schema in lock-step with Qalam.Data.Entity.Messaging.TeacherContentFileUploadMessage.
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
