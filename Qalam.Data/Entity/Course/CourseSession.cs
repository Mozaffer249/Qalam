using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Course;

public class CourseSession : AuditableEntity
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int SessionNumber { get; set; }
    public int DurationMinutes { get; set; }

    [MaxLength(150)]
    public string? Title { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public Course Course { get; set; } = null!;
}
