using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Messaging;

/// <summary>
/// Addresses that must not receive outbound email (hard bounce, invalid domain, etc.).
/// </summary>
public class EmailSuppression
{
    public int Id { get; set; }

    /// <summary>Normalized (trim + lowercase) email address.</summary>
    public string Email { get; set; } = string.Empty;

    public EmailSuppressionReason Reason { get; set; }

    public EmailSuppressionSource Source { get; set; }

    public string? Diagnostic { get; set; }

    public int BounceCount { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastBounceAt { get; set; } = DateTime.UtcNow;
}
