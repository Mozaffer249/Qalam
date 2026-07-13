using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;

namespace Qalam.Data.DTOs.Teacher;

public class TeacherInboxSummaryDto
{
    public TeacherInboxCountsDto Counts { get; set; } = new();
}

public class TeacherInboxCountsDto
{
    public int All { get; set; }
    public int Notified { get; set; }
    public int Viewed { get; set; }
    public int OfferSubmitted { get; set; }
    public int Skipped { get; set; }
}

public class TeacherMySessionListItemDto
{
    public int Id { get; set; }
    public string CourseTitle { get; set; } = null!;
    public string SourceLabel { get; set; } = null!;
    public int SessionNumber { get; set; }
    public string SessionTitle { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public string TeachingMode { get; set; } = "Online";
    public string SessionType { get; set; } = "Individual";
    public int StudentsCount { get; set; }
    public string Status { get; set; } = "Scheduled";
}

public class TeacherMySessionDetailDto : TeacherMySessionListItemDto
{
    public string? Description { get; set; }
    public List<string> UnitNames { get; set; } = new();
    public string? Notes { get; set; }
    public string? ZoomLink { get; set; }
    public List<TeacherSessionStudentDto> Students { get; set; } = new();
    public List<TeacherSessionContentLinkDto> ContentLinks { get; set; } = new();
    public List<TeacherSessionHomeworkDto> Homework { get; set; } = new();
    public DateTime? EndedAt { get; set; }
}

public class TeacherSessionStudentDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? StudentAvatarUrl { get; set; }
    public string Attendance { get; set; } = "Pending";
}

public class TeacherFinanceSummaryDto
{
    public decimal TotalEarningsAllTime { get; set; }
    public decimal EarningsThisMonth { get; set; }
    public decimal EarningsLastMonth { get; set; }
    public decimal PendingPayout { get; set; }
    public DateTime? NextPayoutDate { get; set; }
    public decimal PlatformFeesThisMonth { get; set; }
    public decimal RefundsThisMonth { get; set; }
    public int TransactionsCount { get; set; }
}

public class TeacherFinanceTransactionDto
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = "Payment";
    public string Status { get; set; } = "Completed";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime CreatedAt { get; set; }
    public string Description { get; set; } = null!;
    public string? RelatedStudentName { get; set; }
    public string? RelatedCourseTitle { get; set; }
    public string? InvoiceNumber { get; set; }
}

public class TeacherNotificationsPageDto
{
    public List<TeacherNotificationDto> Items { get; set; } = new();
    public TeacherNotificationCountsDto Counts { get; set; } = new();
}

public class TeacherNotificationCountsDto
{
    public int All { get; set; }
    public int Unread { get; set; }
}

public class TeacherNotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "NewQualifiedRequest";
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string BodyAr { get; set; } = null!;
    public string BodyEn { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public int? RequestId { get; set; }
}
