using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class TeacherDomainQuestionPublicDto
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string RequirementType { get; set; } = null!;
    public bool IsRequired { get; set; }
    public bool RequiresAdminReview { get; set; }
    public int SortOrder { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
    public int MaxFileSizeBytes { get; set; }
    public List<string> AllowedExtensions { get; set; } = new();
    public int? MaxLength { get; set; }
    public List<RequirementOptionDto>? Options { get; set; }
    public bool IsSubmitted { get; set; }
    public DocumentVerificationStatus? VerificationStatus { get; set; }
}

public class EducationDomainTeacherDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool RequiresAnswer { get; set; }
    public List<TeacherDomainQuestionPublicDto> Questions { get; set; } = new();
}

public class TeacherDomainQuestionAdminDto
{
    public int Id { get; set; }
    public int DomainId { get; set; }
    public string DomainCode { get; set; } = null!;
    public string DomainNameEn { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public RegistrationRequirementType RequirementType { get; set; }
    public bool IsActive { get; set; }
    public bool IsRequired { get; set; }
    public bool RequiresAdminReview { get; set; }
    public int SortOrder { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
    public int MaxFileSizeBytes { get; set; }
    public List<string> AllowedExtensions { get; set; } = new();
    public int? MaxLength { get; set; }
    public List<RequirementOptionDto>? Options { get; set; }
    public TeacherDocumentType? MapsToDocumentType { get; set; }
    public bool IsSystem { get; set; }
}

public class CreateTeacherDomainQuestionDto
{
    public int DomainId { get; set; }
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public RegistrationRequirementType RequirementType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRequired { get; set; } = true;
    public bool RequiresAdminReview { get; set; }
    public int SortOrder { get; set; }
    public int MinCount { get; set; } = 1;
    public int MaxCount { get; set; } = 1;
    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public List<string>? AllowedExtensions { get; set; }
    public int? MaxLength { get; set; }
    public List<RequirementOptionDto>? Options { get; set; }
    public TeacherDocumentType? MapsToDocumentType { get; set; }
}

public class UpdateTeacherDomainQuestionDto
{
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; }
    public bool IsRequired { get; set; }
    public bool RequiresAdminReview { get; set; }
    public int SortOrder { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
    public int MaxFileSizeBytes { get; set; }
    public List<string>? AllowedExtensions { get; set; }
    public int? MaxLength { get; set; }
    public List<RequirementOptionDto>? Options { get; set; }
}

public class TeacherDomainQuestionSubmissionStatusDto
{
    public int SubmissionId { get; set; }
    public int QuestionId { get; set; }
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string RequirementType { get; set; } = null!;
    public bool IsRequired { get; set; }
    public bool RequiresAdminReview { get; set; }
    public bool IsSubmitted { get; set; }
    public DocumentVerificationStatus? VerificationStatus { get; set; }
    public string? RejectionReason { get; set; }
    public int? TeacherDocumentId { get; set; }
    public string? TextValue { get; set; }
    public bool? BoolValue { get; set; }
    public List<RequirementOptionDto>? SelectedOptions { get; set; }
}

public class TeacherDomainQuestionGroupDto
{
    public int DomainId { get; set; }
    public string DomainCode { get; set; } = null!;
    public string DomainNameAr { get; set; } = null!;
    public string DomainNameEn { get; set; } = null!;
    public List<TeacherDomainQuestionSubmissionStatusDto> Questions { get; set; } = new();
}

public class SubmitTeacherDomainQuestionsDto
{
    public int DomainId { get; set; }
    public List<TeacherDomainQuestionAnswerDto> Answers { get; set; } = new();
}

public class TeacherDomainQuestionAnswerDto
{
    public string Code { get; set; } = null!;
    public string? TextValue { get; set; }
    public bool? BoolValue { get; set; }
    public List<string> SelectedValues { get; set; } = new();
}

public class TeacherDomainQuestionSubmitResponseDto
{
    public int DomainId { get; set; }
    public bool RequiresAnswer { get; set; }
    public string Message { get; set; } = null!;
    public List<string> SubmittedCodes { get; set; } = new();
}
