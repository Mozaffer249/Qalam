namespace Qalam.Data.DTOs.Admin;

public class AdminTeacherEarningsRecomputeResultDto
{
    public bool DryRun { get; set; }
    public int EnrollmentsScanned { get; set; }
    public int SnapshotsUpdated { get; set; }
    public int LinesCreated { get; set; }
    public int LinesUpdated { get; set; }
    public decimal SnapshotEarningsDelta { get; set; }
    public decimal LineAmountDelta { get; set; }
    public List<AdminTeacherEarningsRecomputeItemDto> Changes { get; set; } = [];
}

public class AdminTeacherEarningsRecomputeItemDto
{
    public int EnrollmentId { get; set; }
    public int TeacherId { get; set; }
    public decimal OldTeacherSharePct { get; set; }
    public decimal NewTeacherSharePct { get; set; }
    public decimal OldTeacherEarnings { get; set; }
    public decimal NewTeacherEarnings { get; set; }
    public decimal OldPlatformShare { get; set; }
    public decimal NewPlatformShare { get; set; }
    public int LinesCreated { get; set; }
    public int LinesUpdated { get; set; }
    public decimal LineAmountDelta { get; set; }
}
