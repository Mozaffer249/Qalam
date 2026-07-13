namespace Qalam.Data.Entity.Teacher;

public class SessionHomeworkFileLink
{
    public int Id { get; set; }
    public int SessionHomeworkAssignmentId { get; set; }
    public int ContentItemId { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    public SessionHomeworkAssignment Assignment { get; set; } = null!;
    public TeacherContentItem ContentItem { get; set; } = null!;
}
