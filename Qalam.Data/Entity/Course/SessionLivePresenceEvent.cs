using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// Append-only LiveKit join/leave record for a session participant.
/// </summary>
public class SessionLivePresenceEvent : AuditableEntity
{
    public long Id { get; set; }

    public int CourseScheduleId { get; set; }

    public LivePresenceRole Role { get; set; }

    /// <summary>Teacher.Id or Student.Id depending on <see cref="Role"/>.</summary>
    public int ParticipantId { get; set; }

    public LivePresenceEventType EventType { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <summary>LiveKit webhook event id (idempotency key).</summary>
    public string LiveKitEventId { get; set; } = string.Empty;

    /// <summary>Raw LiveKit participant identity (e.g. teacher-12).</summary>
    public string Identity { get; set; } = string.Empty;

    public CourseSchedule CourseSchedule { get; set; } = null!;
}
