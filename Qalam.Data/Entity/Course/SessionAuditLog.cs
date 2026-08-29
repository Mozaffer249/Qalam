using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

/// <summary>Append-only audit trail for session lifecycle, complaints, and admin actions.</summary>
public class SessionAuditLog : AuditableEntity
{
    public int Id { get; set; }
    public int CourseScheduleId { get; set; }
    public int ActorUserId { get; set; }
    public string ActorRole { get; set; } = "";
    public SessionAuditActionType ActionType { get; set; }
    public string? PayloadJson { get; set; }

    public CourseSchedule CourseSchedule { get; set; } = null!;
}
