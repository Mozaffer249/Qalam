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
    /// معرف المستخدم الذي قدم الطلب
    /// </summary>
    public int RequestedByUserId { get; set; }

    /// <summary>
    /// طريقة التدريس (يتم تعيينها تلقائياً من الدورة)
    /// </summary>
    public int TeachingModeId { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public int TotalMinutes { get; set; }

    public decimal EstimatedTotalPrice { get; set; }

    [MaxLength(400)]
    public string? Notes { get; set; }

    // Navigation Properties
    public Course Course { get; set; } = null!;
    public Identity.User RequestedByUser { get; set; } = null!;

    public ICollection<CourseRequestSelectedAvailability> SelectedAvailabilities { get; set; } = new List<CourseRequestSelectedAvailability>();
    public ICollection<CourseRequestGroupMember> GroupMembers { get; set; } = new List<CourseRequestGroupMember>();
    public ICollection<CourseRequestProposedSession> ProposedSessions { get; set; } = new List<CourseRequestProposedSession>();
}
