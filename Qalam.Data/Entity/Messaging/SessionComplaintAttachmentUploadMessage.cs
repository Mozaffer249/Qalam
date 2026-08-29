namespace Qalam.Data.Entity.Messaging;

/// <summary>
/// RabbitMQ payload published when a student uploads a session complaint attachment.
/// MessagingApi uploads to OSS at the pre-computed <see cref="StorageKey"/>.
/// The API stores the public URL on <see cref="SessionComplaintAttachment"/> before queueing.
/// </summary>
public class SessionComplaintAttachmentUploadMessage
{
    public int ComplaintId { get; set; }
    public int AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string FileData { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
}
