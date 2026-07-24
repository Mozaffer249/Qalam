using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class SessionReviewDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; }
    /// <summary>StudentToTeacher or TeacherToStudent.</summary>
    public string Direction { get; set; } = "StudentToTeacher";
}

public class SubmitSessionReviewRequestDto
{
    public int Rating { get; set; }
    public string? Feedback { get; set; }
}

public class SetStudentSessionReviewRequestDto
{
    public int StudentId { get; set; }
    public decimal Rating { get; set; }
    public string? Note { get; set; }
}
