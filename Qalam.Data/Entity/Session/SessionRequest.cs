using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Data.Entity.Session;

/// <summary>
/// طلب جلسة من الطالب (يتلقى عروض من المعلمين)
/// </summary>
public class SessionRequest : AuditableEntity
{
    public int Id { get; set; }
    
    public int StudentId { get; set; }
    
    // المحتوى التعليمي
    public int SubjectId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    
    /// <summary>
    /// طريقة التدريس المطلوبة
    /// </summary>
    public int TeachingModeId { get; set; }
    
    /// <summary>
    /// نوع الجلسة (فردي/جماعي)
    /// </summary>
    public int SessionTypeId { get; set; }
    
    /// <summary>
    /// الموقع (للتدريس الحضوري)
    /// </summary>
    public int? LocationId { get; set; }
    
    [MaxLength(800)]
    public string? Description { get; set; }
    
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    
    // Navigation Properties
    public Student.Student Student { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Curriculum? Curriculum { get; set; }
    public EducationLevel? Level { get; set; }
    public TeachingMode TeachingMode { get; set; } = null!;
    public SessionType SessionType { get; set; } = null!;
    public Location? Location { get; set; }
    
    public ICollection<SessionRequestOffer> Offers { get; set; } = new List<SessionRequestOffer>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
