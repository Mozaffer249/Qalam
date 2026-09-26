namespace Qalam.MessagingApi.Models.Entities;

public class ProfilePicUploadMessage
{
    public int UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileData { get; set; } = string.Empty; // Base64 encoded
    /// <summary>
    /// Precomputed identities OSS key (e.g. <c>profiles/{userId}/{guid}.jpg</c>).
    /// When set, MessagingApi uploads to this key instead of inventing a new one.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;
    /// <summary>Prior OSS/public URL to delete after a successful replace upload.</summary>
    public string? PreviousFileUrl { get; set; }
    public DateTime QueuedAt { get; set; }
}
