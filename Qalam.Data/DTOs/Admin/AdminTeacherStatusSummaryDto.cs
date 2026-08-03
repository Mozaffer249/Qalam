namespace Qalam.Data.DTOs.Admin;

/// <summary>Aggregate teacher counts for the admin teachers list status cards.</summary>
public class AdminTeacherStatusSummaryDto
{
    public int Total { get; set; }
    public int AwaitingDocuments { get; set; }
    public int PendingVerification { get; set; }
    public int DocumentsRejected { get; set; }
    public int Active { get; set; }
    public int Blocked { get; set; }

    /// <summary>
    /// Active teachers with subjects + availability who are held by the platform launch gate.
    /// Zero when <c>teacherDashboardReady</c> is true.
    /// </summary>
    public int AwaitingPlatformLaunch { get; set; }
}
