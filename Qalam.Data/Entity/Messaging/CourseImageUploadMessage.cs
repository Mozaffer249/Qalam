namespace Qalam.Data.Entity.Messaging;

/// <summary>
/// RabbitMQ payload published by Qalam.Api when a teacher uploads a course cover image.
/// MessagingApi consumes it and uploads to the learning OSS bucket at the pre-computed StorageKey.
/// </summary>
public class CourseImageUploadMessage
{
    public int TeacherId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    /// <summary>OSS object key under the learning bucket (e.g. courses/{teacherId}/{guid}.jpg).</summary>
    public string StorageKey { get; set; } = string.Empty;
    /// <summary>Base64-encoded file bytes.</summary>
    public string FileData { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
}
