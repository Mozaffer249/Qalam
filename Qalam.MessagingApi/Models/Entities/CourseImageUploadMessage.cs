namespace Qalam.MessagingApi.Models.Entities;

/// <summary>
/// Consumer-side copy of the course image upload payload published by Qalam.Api.
/// Keep schema in lock-step with Qalam.Data.Entity.Messaging.CourseImageUploadMessage.
/// </summary>
public class CourseImageUploadMessage
{
    public int TeacherId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string FileData { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
}
