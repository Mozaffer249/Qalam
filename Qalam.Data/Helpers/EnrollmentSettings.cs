namespace Qalam.Data.Helpers;

public class EnrollmentSettings
{
    public int PaymentDeadlineHours { get; set; } = 48;
    /// <summary>
    /// Hours after invite creation before a pending external invite expires (S1 Cancelled / S2 Expired).
    /// </summary>
    public int InviteResponseDeadlineHours { get; set; } = 48;
    public int ExpirationCheckIntervalMinutes { get; set; } = 5;
}
