using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// استثناءات توفر المعلم (إجازة أو وقت إضافي)
/// </summary>
public class TeacherAvailabilityException : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherId { get; set; }
    
    /// <summary>
    /// تاريخ الاستثناء
    /// </summary>
    public DateOnly Date { get; set; }
    
    public int TimeSlotId { get; set; }
    
    /// <summary>
    /// نوع الاستثناء (محجوب أو إضافي)
    /// </summary>
    public AvailabilityExceptionType ExceptionType { get; set; }
    
    [MaxLength(250)]
    public string? Reason { get; set; }
    
    // Navigation Properties
    public Teacher Teacher { get; set; } = null!;
    public TimeSlot TimeSlot { get; set; } = null!;
}
