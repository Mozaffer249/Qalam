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
