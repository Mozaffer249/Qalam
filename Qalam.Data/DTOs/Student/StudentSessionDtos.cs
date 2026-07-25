using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Student;

/// <summary>Student (or guardian) view of a single course schedule session.</summary>
public class StudentSessionDetailDto
{
    public int ScheduleId { get; set; }
    public int EnrollmentId { get; set; }
    public int SessionNumber { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public string? TeacherNote { get; set; }
    public DateOnly? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public ScheduleStatus Status { get; set; }
    public bool CanJoin { get; set; }
    public string? AttendanceStatus { get; set; }
    public List<EnrollmentSessionContentUnitDto> Units { get; set; } = new();
    public List<SessionReviewDto> Reviews { get; set; } = new();
}
