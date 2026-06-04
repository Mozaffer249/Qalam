using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

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
}

public class TeacherRegistrationStatusResponseDto
{
    public List<TeacherRegistrationSubmissionStatusDto> Requirements { get; set; } = new();
    public List<TeacherDocumentReviewDto> LegacyDocuments { get; set; } = new();
}
