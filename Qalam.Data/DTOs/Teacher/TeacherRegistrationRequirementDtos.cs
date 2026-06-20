using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

/// <summary>One choice on a Selection requirement.</summary>
public class RequirementOptionDto
{
    public string Value { get; set; } = null!;
    public string LabelAr { get; set; } = null!;
    public string LabelEn { get; set; } = null!;
}

/// <summary>Public / teacher-facing requirement (active only).</summary>
public class TeacherRegistrationRequirementPublicDto
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string RequirementType { get; set; } = null!;
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
    public int MaxFileSizeBytes { get; set; }
    public List<string> AllowedExtensions { get; set; } = new();
    public int? MaxLength { get; set; }
    /// <summary>Only populated for Selection-type requirements.</summary>
    public List<RequirementOptionDto>? Options { get; set; }
}

public class TeacherRegistrationRequirementsResponseDto
{
    public List<TeacherRegistrationRequirementPublicDto> Requirements { get; set; } = new();
}

/// <summary>Admin list/detail.</summary>
public class TeacherRegistrationRequirementAdminDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public RegistrationRequirementType RequirementType { get; set; }
    public bool IsActive { get; set; }
    public bool IsRequired { get; set; }
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

public class CreateTeacherRegistrationRequirementDto
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public RegistrationRequirementType RequirementType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }
    public int MinCount { get; set; } = 1;
    public int MaxCount { get; set; } = 1;
    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public List<string>? AllowedExtensions { get; set; }
    public int? MaxLength { get; set; }
    /// <summary>Required when <c>RequirementType == Selection</c>. Bilingual picklist items.</summary>
    public List<RequirementOptionDto>? Options { get; set; }
    public TeacherDocumentType? MapsToDocumentType { get; set; }
}

public class UpdateTeacherRegistrationRequirementDto
{
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
    public int MaxFileSizeBytes { get; set; }
    public List<string>? AllowedExtensions { get; set; }
    public int? MaxLength { get; set; }
    /// <summary>Replaces stored options when the requirement is Selection-type.</summary>
    public List<RequirementOptionDto>? Options { get; set; }
}

public class SetRequirementActiveDto
{
    public bool IsActive { get; set; }
}

/// <summary>Per-requirement status for teacher documents screen.</summary>
public class TeacherRegistrationSubmissionStatusDto
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string RequirementType { get; set; } = null!;
    public bool IsRequired { get; set; }
    public bool IsSubmitted { get; set; }
    public DocumentVerificationStatus? VerificationStatus { get; set; }
    public string? RejectionReason { get; set; }
    public int? TeacherDocumentId { get; set; }
    public string? TextValue { get; set; }
    public bool? BoolValue { get; set; }
    /// <summary>Populated only for Selection-type requirements: chosen option values + their resolved bilingual labels.</summary>
    public List<RequirementOptionDto>? SelectedOptions { get; set; }
}

public class TeacherRegistrationStatusResponseDto
{
    public TeacherStatus TeacherStatus { get; set; }
    public bool IsAccountActivated { get; set; }
    public bool CanBeActivated { get; set; }
    public bool AwaitingFinalApproval { get; set; }
    public bool RequiresAvailabilitySetup { get; set; }
    public TeacherSubjectSummaryDto SubjectSummary { get; set; } = new();
    public List<TeacherRegistrationSubmissionStatusDto> Requirements { get; set; } = new();
    public List<TeacherDocumentReviewDto> LegacyDocuments { get; set; } = new();
}

public class TeacherAccountStatusResponseDto
{
    public TeacherStatus TeacherStatus { get; set; }
    public bool IsAccountActivated { get; set; }
    public bool CanBeActivated { get; set; }
    public bool AwaitingFinalApproval { get; set; }
    public bool RequiresAvailabilitySetup { get; set; }
    public RegistrationStepDto NextStep { get; set; } = null!;
}
