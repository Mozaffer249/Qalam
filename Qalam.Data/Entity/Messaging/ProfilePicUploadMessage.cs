namespace Qalam.Data.Entity.Messaging;

public class ProfilePicUploadMessage
{
    public int UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileData { get; set; } = string.Empty; // Base64 encoded
    /// <summary>Prior OSS/public URL to delete after a successful replace upload.</summary>
    public string? PreviousFileUrl { get; set; }
    public DateTime QueuedAt { get; set; }
}
