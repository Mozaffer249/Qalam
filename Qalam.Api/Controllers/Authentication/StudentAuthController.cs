using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Authentication.Commands.StudentRegistration;
using Qalam.Core.Features.Student.Commands.AddChild;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Student;

namespace Qalam.Api.Controllers.Authentication;

/// <summary>
/// Student and Parent registration and authentication
/// </summary>
[ApiController]
[Route("Api/V1/Authentication/Student")]
public class StudentAuthController : AppControllerBase
{
    /// <summary>
    /// Send OTP (Screen 1 - phone only). Response includes IsNewUser, masked phone, and message (login vs registration).
    /// </summary>
    [HttpPost("SendOtp")]
    [ProducesResponseType(typeof(StudentSendOtpResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendOtp([FromBody] StudentSendOtpCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Verify OTP (Screen 2). Returns token and NextStep (ChooseAccountType or Dashboard). No account creation yet.
    /// </summary>
    /// <remarks>
    /// Response includes:
    /// - **NextStepName**: Primary next step (e.g., "ChooseAccountType", "Dashboard")
    /// - **IsNextStepRequired**: Whether the next step is mandatory or optional
    /// - **OptionalSteps**: List of optional steps available to the user
    /// - **NextStepDescription**: Clear description of what to do next
    /// </remarks>
    [HttpPost("VerifyOtp")]
    [ProducesResponseType(typeof(StudentRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp([FromBody] StudentVerifyOtpCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Set account type and usage (Screen 3 + 4). Requires auth (token from VerifyOtp). Validates 18+, creates Student/Guardian.
    /// </summary>
    /// <remarks>
    /// This endpoint completes the registration by setting the account type and usage preferences.
    /// 
    /// **Account Types:**
    /// - **Student**: Register as a student only (can learn and enroll in courses)
    /// - **Parent**: Register as a parent/guardian only (can add and manage children)
    /// - **Both**: Register as both student and parent (can learn themselves AND add/manage children)
    /// 
    /// **Usage Modes** (Required for Parent or Both):
    /// - **StudySelf**: Parent will study/learn themselves
    /// - **AddChildren**: Parent will only add/manage children (not study)
    /// - **Both**: Parent will study themselves AND add/manage children
    /// 
    /// **Validation Rules:**
    /// - User must be 18+ years old (DateOfBirth validation)
    /// - AccountType is required and case-insensitive
    /// - UsageMode is required when AccountType is "Parent" or "Both"
    /// - Email must be valid and unique
    /// - Password must meet security requirements
    /// 
    /// **Example Requests:**
    /// 
    /// Student only:
    /// ```json
    /// {
    ///   "data": {
    ///     "accountType": "Student",
    ///     "firstName": "Ahmed",
    ///     "lastName": "Ali",
    ///     "email": "ahmed@example.com",
    ///     "password": "SecurePass123!",
    ///     "dateOfBirth": "2000-01-15"
    ///   }
    /// }
    /// ```
    /// 
    /// Parent who will study:
    /// ```json
    /// {
    ///   "data": {
    ///     "accountType": "Parent",
    ///     "usageMode": "StudySelf",
    ///     "firstName": "Fatima",
    ///     "lastName": "Hassan",
    ///     "email": "fatima@example.com",
    ///     "password": "SecurePass123!",
    ///     "dateOfBirth": "1985-03-20",
    ///     "cityOrRegion": "Riyadh"
    ///   }
    /// }
    /// ```
    /// 
    /// Both student and parent:
    /// ```json
    /// {
    ///   "data": {
    ///     "accountType": "Both",
    ///     "usageMode": "Both",
    ///     "firstName": "Mohammed",
    ///     "lastName": "Ibrahim",
    ///     "email": "mohammed@example.com",
    ///     "password": "SecurePass123!",
    ///     "dateOfBirth": "1990-07-10"
    ///   }
    /// }
    /// ```
    /// 
    /// **Response includes:**
    /// - **NextStepName**: Primary next step (e.g., "CompleteAcademicProfile", "AddChildren", "Dashboard")
    /// - **IsNextStepRequired**: Whether the next step is mandatory or optional
    /// - **OptionalSteps**: List of optional steps available (e.g., ["AddChildren"], ["Dashboard"])
    /// - **NextStepDescription**: Clear description of what to do next
    /// 
    /// The response intelligently determines the next step based on AccountType and UsageMode:
    /// - Student only → Must complete academic profile
    /// - Parent + StudySelf → Must complete academic profile, can add children later
    /// - Parent + AddChildren → Can add children or skip to dashboard
    /// - Parent + Both → Must complete academic profile, can add children later
    /// - Both → Must complete academic profile, can add children anytime
    /// </remarks>
    /// <response code="200">Registration completed successfully. Returns token and next step information.</response>
    /// <response code="400">Invalid request data or validation errors (e.g., under 18, invalid email, weak password).</response>
    /// <response code="401">Unauthorized - token from VerifyOtp is invalid or expired.</response>
    [HttpPost("SetAccountTypeAndUsage")]
    [Authorize]
    [ProducesResponseType(typeof(StudentRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetAccountTypeAndUsage([FromBody] SetAccountTypeAndUsageCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Complete academic profile (Domain, Curriculum, Level, Grade) for student or parent who studies.
    /// </summary>
    /// <remarks>
    /// Response includes:
    /// - **NextStepName**: Always "Dashboard" after completing profile
    /// - **IsNextStepRequired**: false (no further required steps)
    /// - **OptionalSteps**: ["AddChildren"] if user is also a guardian, empty otherwise
    /// - **NextStepDescription**: Clear description of completion status
    /// </remarks>
    [HttpPost("CompleteProfile")]
    [Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
    [ProducesResponseType(typeof(StudentRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteStudentProfileCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Parent adds a child (Student linked to Guardian).
    /// </summary>
    [HttpPost("AddChild")]
    [Authorize(Roles = Roles.Guardian)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddChild([FromBody] AddChildCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }
}
