using Qalam.Data.Entity.Course;

namespace Qalam.Data.Entity.Teacher;

public class SessionHomeworkAssignment
{
    public int Id { get; set; }
    public int CourseScheduleId { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public CourseSchedule CourseSchedule { get; set; } = null!;
    public ICollection<SessionHomeworkFileLink> FileLinks { get; set; } = new List<SessionHomeworkFileLink>();
}
