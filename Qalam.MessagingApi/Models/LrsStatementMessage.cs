namespace Qalam.MessagingApi.Models;

public class LrsStatementMessage
{
    public int? LogId { get; set; }
    public string StatementJson { get; set; } = string.Empty;
    public string Verb { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SourceEntityType { get; set; } = string.Empty;
    public int SourceEntityId { get; set; }
    public DateTime QueuedAt { get; set; }
}
