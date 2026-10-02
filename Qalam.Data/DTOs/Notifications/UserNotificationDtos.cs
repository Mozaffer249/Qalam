namespace Qalam.Data.DTOs.Notifications;

public class UserNotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string TitleAr { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public string BodyAr { get; set; } = "";
    public string BodyEn { get; set; } = "";
    /// <summary>Raw JSON deep-link payload (enrollmentId, scheduleId, policyCaseId…).</summary>
    public string? DataJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserNotificationsPageDto
{
    public List<UserNotificationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
