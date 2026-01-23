namespace Qalam.Data.DTOs.Teacher;

public class RegistrationStepDto
{
    public int CurrentStep { get; set; }
    public int NextStep { get; set; }
    public string NextStepName { get; set; } = string.Empty;
    public bool IsRegistrationComplete { get; set; }
    public string? Message { get; set; }
    
    /// <summary>
    /// List of rejected documents (only populated when Status = DocumentsRejected)
    /// </summary>
    public List<RejectedDocumentInfo>? RejectedDocuments { get; set; }
}

/// <summary>
/// Information about a rejected document
/// </summary>
public class RejectedDocumentInfo
{
    public int DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
}
