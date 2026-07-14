using Qalam.Data.Entity.Teacher;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// Library file linked to a fixed-course curriculum session (shared by all schedules with that CourseSessionId).
/// </summary>
public class CourseSessionContentLink
{
    public int Id { get; set; }
    public int CourseSessionId { get; set; }
    public int ContentItemId { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    public CourseSession CourseSession { get; set; } = null!;
    public TeacherContentItem ContentItem { get; set; } = null!;
}
