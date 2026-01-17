using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// طلب التسجيل في دورة
/// </summary>
public class CourseEnrollmentRequest : AuditableEntity
{
    public int Id { get; set; }
    
    public int CourseId { get; set; }
    
    /// <summary>
    /// معرف الطالب الذي قدم الطلب
    /// </summary>
    public int RequestedByStudentId { get; set; }
    
    /// <summary>
    /// طريقة التدريس المطلوبة
    /// </summary>
    public int TeachingModeId { get; set; }
    
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    
    [MaxLength(400)]
    public string? Notes { get; set; }
    
    // Navigation Properties
    public Course Course { get; set; } = null!;
    public Student.Student RequestedByStudent { get; set; } = null!;
    
    public ICollection<CourseRequestSelectedAvailability> SelectedAvailabilities { get; set; } = new List<CourseRequestSelectedAvailability>();
    public ICollection<CourseRequestGroupMember> GroupMembers { get; set; } = new List<CourseRequestGroupMember>();
}
