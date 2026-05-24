namespace Qalam.Data.Entity.Messaging;

/// <summary>
/// RabbitMQ payload published by Qalam.Api when a student uploads an attachment on
/// an Open Session Request. MessagingApi consumes it, uploads to OSS, then writes
/// the resulting URL back to sr.SessionRequestAttachments.
/// </summary>
public class OpenSessionRequestAttachmentUploadMessage
{
    public int OpenSessionRequestId { get; set; }
    public int AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    /// <summary>
    /// OSS object key. Pre-computed by the API handler so the consumer doesn't need to write
    /// back to the main DB (it's a different DB connection in MessagingApi).
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;
    /// <summary>Base64-encoded file bytes.</summary>
    public string FileData { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
}
