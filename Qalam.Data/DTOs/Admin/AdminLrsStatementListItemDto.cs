using Qalam.Data.Entity.LRS;

namespace Qalam.Data.DTOs.Admin;

public class AdminLrsStatementListItemDto
{
    public int Id { get; set; }
    public Guid StatementId { get; set; }
    public string Verb { get; set; } = string.Empty;
    public string ActorIdentifier { get; set; } = string.Empty;
    public string ObjectIri { get; set; } = string.Empty;
    public LrsStatementStatus Status { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string SourceEntityType { get; set; } = string.Empty;
    public int SourceEntityId { get; set; }

    public string? StudentName { get; set; }
    public string? CourseName { get; set; }
    public DateOnly? SessionDate { get; set; }
    public int? EnrollmentId { get; set; }
}
