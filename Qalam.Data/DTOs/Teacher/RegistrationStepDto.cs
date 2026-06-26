using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class RegistrationStepDto
{
    public int CurrentStep { get; set; }
    public int NextStep { get; set; }
    public string NextStepName { get; set; } = string.Empty;
    public bool IsRegistrationComplete { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// True when account is Active but weekly availability has not been configured yet.
    /// </summary>
    public bool RequiresAvailabilitySetup { get; set; }

    /// <summary>
    /// True when all docs and subjects are approved but admin has not activated the account yet.
    /// </summary>
    public bool AwaitingFinalApproval { get; set; }

    /// <summary>
    /// List of rejected documents (only populated when Status = DocumentsRejected)
    /// </summary>
    public List<RejectedDocumentInfo>? RejectedDocuments { get; set; }

    /// <summary>
    /// Actionable review items for routing (domain questions, direct subject rejects, etc.)
    /// </summary>
    public List<TeacherReviewCorrectionDto>? PendingCorrections { get; set; }
}

public enum TeacherReviewCorrectionType
{
    RegistrationDocument = 1,
    DomainQuestion = 2,
    Subject = 3
}

public class TeacherReviewCorrectionDto
{
    public TeacherReviewCorrectionType Type { get; set; }
    public int? DomainId { get; set; }
    public string? DomainCode { get; set; }
    public string? Label { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public int? SubmissionId { get; set; }
    public int? TeacherSubjectId { get; set; }
    public int? DocumentId { get; set; }
}

/// <summary>
/// Information about a rejected document
/// </summary>
public class RejectedDocumentInfo
{
    public int DocumentId { get; set; }
    public TeacherDocumentType DocumentType { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
}
