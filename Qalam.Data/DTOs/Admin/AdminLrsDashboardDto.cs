namespace Qalam.Data.DTOs.Admin;

public class AdminLrsDashboardDto
{
    public int TotalStatements { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int QueuedCount { get; set; }
    public int DeadLetteredCount { get; set; }
    public DateTime? LastSentAt { get; set; }
    public Dictionary<string, int> StatementsByVerb { get; set; } = new();
    public decimal FailureRate { get; set; }
}
