using Qalam.Data.Entity.Course;

namespace Qalam.Data.Entity.Teacher;

public class SessionContentLink
{
    public int Id { get; set; }
    public int CourseScheduleId { get; set; }
    public int ContentItemId { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    public CourseSchedule CourseSchedule { get; set; } = null!;
    public TeacherContentItem ContentItem { get; set; } = null!;
}
