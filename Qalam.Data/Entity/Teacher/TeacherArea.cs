using Qalam.Data.Commons;
using Qalam.Data.Entity.Common;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// المناطق الجغرافية التي يخدمها المعلم (للتدريس الحضوري)
/// </summary>
public class TeacherArea : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherId { get; set; }
    public int LocationId { get; set; }
    
    /// <summary>
    /// أقصى مسافة يمكن للمعلم التنقل إليها (بالكيلومتر)
    /// </summary>
    public decimal MaxDistanceKm { get; set; } = 0m;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public Teacher Teacher { get; set; } = null!;
    public Location Location { get; set; } = null!;
}
