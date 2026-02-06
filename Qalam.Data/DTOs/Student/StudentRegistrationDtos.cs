using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Student;

#region Registration Request

/// <summary>
/// Account type for registration
/// </summary>
public enum StudentAccountType
{
    Student = 1,
    Parent = 2,
    Both = 3
}

/// <summary>
/// Usage mode for parent (Screen 4): study self, add children, or both
/// </summary>
public enum UsageMode
{
    StudySelf = 1,
    AddChildren = 2,
    Both = 3
}

/// <summary>
/// Request for SendOtp - phone only (Screen 1, no account type or DOB yet)
/// </summary>
public class StudentSendOtpRequestDto
{
    public string CountryCode { get; set; } = "+966";
    public string PhoneNumber { get; set; } = default!;
}

/// <summary>
/// DTO for student/parent registration - basic info + DOB + account type
/// </summary>
public class StudentRegistrationDto
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? CityOrRegion { get; set; }
    
    /// <summary>
    /// Date of birth - must be 18+ to register
    /// </summary>
    public DateOnly DateOfBirth { get; set; }
    
    /// <summary>
    /// Student, Parent, or Both
    /// </summary>
    public StudentAccountType AccountType { get; set; }
}

/// <summary>
/// Request for SetAccountTypeAndUsage (Screen 3 + 4) - after VerifyOtp, with 18+ validation
/// </summary>
public class SetAccountTypeAndUsageDto
{
    public StudentAccountType AccountType { get; set; }
    /// <summary>For parent: StudySelf, AddChildren, or Both</summary>
    public UsageMode? UsageMode { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    /// <summary>From VerifyOtp response when user was just created; required to set password</summary>
    public string? PasswordSetupToken { get; set; }
    public string? CityOrRegion { get; set; }
    /// <summary>Must be 18+</summary>
    public DateOnly DateOfBirth { get; set; }
}

#endregion

#region Academic Profile

/// <summary>
/// DTO for academic profile wizard (Domain, Curriculum, Level, Grade)
/// </summary>
public class StudentAcademicProfileDto
{
    public int DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
}

#endregion

#region Add Child (Parent)

/// <summary>
/// DTO for parent to add a child
/// </summary>
public class AddChildDto
{
    public string FullName { get; set; } = default!;
    public DateOnly DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public GuardianRelation? GuardianRelation { get; set; }
    
    public int? DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
}

#endregion

#region Responses

/// <summary>
/// Response with next step after OTP verify or profile completion
/// </summary>
public class StudentRegistrationResponseDto
{
    public string? Token { get; set; }
    public int CurrentStep { get; set; }
    public string NextStepName { get; set; } = default!;
    public bool IsRegistrationComplete { get; set; }
    public string? Message { get; set; }
    /// <summary>When VerifyOtp creates a new user; client sends this back with SetAccountTypeAndUsage to set password</summary>
    public string? PasswordSetupToken { get; set; }
}

/// <summary>
/// Response for SendOtp (Screen 1 - phone only). IsNewUser and message indicate login vs registration path.
/// </summary>
public class StudentSendOtpResponseDto
{
    public bool IsNewUser { get; set; }
    public string Message { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
}

#endregion
