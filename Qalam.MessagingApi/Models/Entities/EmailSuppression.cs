using Qalam.MessagingApi.Models.Enums;

namespace Qalam.MessagingApi.Models.Entities;

/// <summary>Maps messaging.EmailSuppressions (created by Infrastructure migrations).</summary>
public class EmailSuppression
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public EmailSuppressionReason Reason { get; set; }
    public EmailSuppressionSource Source { get; set; }
    public string? Diagnostic { get; set; }
    public int BounceCount { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastBounceAt { get; set; } = DateTime.UtcNow;
}
