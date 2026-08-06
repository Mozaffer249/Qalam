using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Teacher entity represents a teacher in the system
/// </summary>
public class Teacher : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// UserId of the teacher
    /// </summary>
    public int? UserId { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    public TeacherStatus Status { get; set; } = TeacherStatus.AwaitingDocuments;

    /// <summary>
    /// Status to restore when admin unblocks the account (set when blocking).
    /// </summary>
    public TeacherStatus? StatusBeforeBlock { get; set; }

    public TeacherLocation? Location { get; set; }

    /// <summary>
    /// Average rating of the teacher (0-5)
    /// </summary>
    public decimal RatingAverage { get; set; } = 0m;

    public bool IsActive { get; set; } = true;

    /// <summary>Whether the teacher offers online sessions.</summary>
    public bool OffersOnline { get; set; } = true;

    /// <summary>Whether the teacher offers in-person sessions.</summary>
    public bool OffersInPerson { get; set; } = false;

    /// <summary>Whether the teacher offers individual (1:1) sessions.</summary>
    public bool OffersIndividual { get; set; } = true;

    /// <summary>Whether the teacher offers group sessions.</summary>
    public bool OffersGroup { get; set; } = false;

    [MaxLength(200)]
    public string? JobTitle { get; set; }

    public int YearsOfExperience { get; set; }

    // Navigation Properties
    public User? User { get; set; }
    public ICollection<TeacherDocument> TeacherDocuments { get; set; } = new List<TeacherDocument>();
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    public ICollection<TeacherAvailability> TeacherAvailabilities { get; set; } = new List<TeacherAvailability>();
    public ICollection<TeacherAvailabilityException> TeacherAvailabilityExceptions { get; set; } = new List<TeacherAvailabilityException>();
    public ICollection<TeacherArea> TeacherAreas { get; set; } = new List<TeacherArea>();
    public ICollection<TeacherReview> TeacherReviews { get; set; } = new List<TeacherReview>();
    public ICollection<TeacherAuditLog> TeacherAuditLogs { get; set; } = new List<TeacherAuditLog>();
    public ICollection<Course.Course> Courses { get; set; } = new List<Course.Course>();
}
