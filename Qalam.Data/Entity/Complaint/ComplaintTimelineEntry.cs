using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Complaint;

public class ComplaintTimelineEntry : AuditableEntity
{
    public int Id { get; set; }
    public int ComplaintId { get; set; }
    public ComplaintTimelineEventType EventType { get; set; }
    public ComplaintStatus? FromStatus { get; set; }
    public ComplaintStatus? ToStatus { get; set; }
    public int ActorUserId { get; set; }
    public string ActorRole { get; set; } = "";
    public string? Notes { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime OccurredAt { get; set; }

    public Complaint Complaint { get; set; } = null!;
}
