using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Session;

/// <summary>
/// جلسة تعليمية (بعد قبول العرض)
/// </summary>
public class Session : AuditableEntity
{
    public int Id { get; set; }
    
    public int SessionRequestId { get; set; }
    
    public int TeacherId { get; set; }
    
    /// <summary>
    /// الطالب الأساسي/المالك
    /// </summary>
    public int StudentId { get; set; }
    
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;
    
    // Navigation Properties
    public SessionRequest SessionRequest { get; set; } = null!;
    public Teacher.Teacher Teacher { get; set; } = null!;
    public Student.Student Student { get; set; } = null!;
    
    public ICollection<ScheduledSession> ScheduledSessions { get; set; } = new List<ScheduledSession>();
}
