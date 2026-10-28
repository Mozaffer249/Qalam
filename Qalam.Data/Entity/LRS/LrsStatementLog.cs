using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qalam.Data.Entity.LRS;

public class LrsStatementLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid StatementId { get; set; }

    [MaxLength(64)]
    public string Verb { get; set; } = string.Empty;

    [MaxLength(256)]
    public string ActorIdentifier { get; set; } = string.Empty;

    [MaxLength(512)]
    public string ObjectIri { get; set; } = string.Empty;

    public int? HttpStatusCode { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public LrsStatementStatus Status { get; set; } = LrsStatementStatus.Queued;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    [MaxLength(64)]
    public string SourceEntityType { get; set; } = string.Empty;

    public int SourceEntityId { get; set; }

    public string? StatementJson { get; set; }
}

public enum LrsStatementStatus
{
    Queued = 0,
    Sent = 1,
    Failed = 2,
    DeadLettered = 3
}
