using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Admin;

public class FailedEmailContactDto
{
    public Guid MessageLogId { get; set; }
    public string Email { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public bool IsSuppressed { get; set; }
}

public class EmailSuppressionListItemDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public EmailSuppressionReason Reason { get; set; }
    public EmailSuppressionSource Source { get; set; }
    public string? Diagnostic { get; set; }
    public int BounceCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastBounceAt { get; set; }
}
