using Qalam.Data.Entity.LRS;

namespace Qalam.Data.DTOs.Admin;

public class AdminLrsStatementFilter
{
    public LrsStatementStatus? Status { get; set; }
    public string? Verb { get; set; }
    public int? StudentId { get; set; }
    public int? EnrollmentId { get; set; }
    public int? CourseScheduleId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
