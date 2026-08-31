namespace Qalam.Data.DTOs.Admin;

public class PartialDomainActivationCandidateDto
{
    public int TeacherId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public int ApprovedDomainCount { get; set; }
    public int RejectedDomainCount { get; set; }
}

public class BulkActivatePartialDomainTeachersRequestDto
{
    public List<int> TeacherIds { get; set; } = new();
}

public class BulkActivatePartialDomainTeachersResultDto
{
    public int ActivatedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<BulkActivateTeacherFailureDto> Failures { get; set; } = new();
}

public class BulkActivateTeacherFailureDto
{
    public int TeacherId { get; set; }
    public string FullName { get; set; } = null!;
    public string ErrorMessage { get; set; } = null!;
}

public class PendingVerificationTeacherSummaryDto
{
    public int TeacherId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
}
