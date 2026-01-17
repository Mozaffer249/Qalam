using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Student;

/// <summary>
/// جدول العلاقة بين الطالب وولي الأمر (Many-to-Many)
/// </summary>
public class StudentGuardian : AuditableEntity
{
    public int Id { get; set; }
    
    public int StudentId { get; set; }
    public int GuardianId { get; set; }
    
    /// <summary>
    /// علاقة ولي الأمر بالطالب (أب، أم، أخ، ...)
    /// </summary>
    public GuardianRelation Relation { get; set; }
    
    /// <summary>
    /// هل هو ولي الأمر الأساسي؟
    /// </summary>
    public bool IsPrimary { get; set; } = false;
    
    // Navigation Properties
    public Student Student { get; set; } = null!;
    public Guardian Guardian { get; set; } = null!;
}
