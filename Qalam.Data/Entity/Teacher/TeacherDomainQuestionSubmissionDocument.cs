namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Links additional (or all) TeacherDocuments to a domain question submission.
/// Submission.TeacherDocumentId remains the primary/first document for backward compatibility.
/// </summary>
public class TeacherDomainQuestionSubmissionDocument
{
    public int SubmissionId { get; set; }
    public int TeacherDocumentId { get; set; }

    public TeacherDomainQuestionSubmission Submission { get; set; } = null!;
    public TeacherDocument TeacherDocument { get; set; } = null!;
}
