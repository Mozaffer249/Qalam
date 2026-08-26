namespace Qalam.Data.DTOs.Admin;

public class AdminStudentFreeTrialConsumptionDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Source { get; set; } = "";
    public int EnrollmentId { get; set; }
    public int? OpenSessionRequestId { get; set; }
    public int TeacherId { get; set; }
    public int DomainId { get; set; }
    public int? CourseScheduleId { get; set; }
    public string Status { get; set; } = "";
    public DateTime ReservedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool RestoredEligibility { get; set; }
    public string? CancelReason { get; set; }
    public int? CancelledByUserId { get; set; }
}

public class AdminTeacherInterviewUnlockDto
{
    public int DomainId { get; set; }
    public string? DomainNameEn { get; set; }
    public string? DomainNameAr { get; set; }
    public bool HasCompletedInterviewSession { get; set; }
    public string InterviewUnlockSource { get; set; } = "";
    public int? InterviewUnlockEnrollmentId { get; set; }
    public int? InterviewUnlockCourseScheduleId { get; set; }
    public DateTime? InterviewUnlockedAt { get; set; }
    public DateTime? InterviewRevertedAt { get; set; }
    public int? TeacherLevelId { get; set; }
}
