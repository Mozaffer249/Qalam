using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Authentication.Queries.GetAuthConfig;
using Qalam.Data.DTOs.Auth;

namespace Qalam.Api.Controllers.Authentication;

/// <summary>
/// Public auth configuration for frontend apps (teacher/student login UI).
/// </summary>
[ApiController]
[Route("Api/V1/Authentication")]
[Tags("Authentication Config (Public)")]
public class AuthenticationConfigController : AppControllerBase
{
    /// <summary>
    /// Get public auth settings for frontend (login/register UI).
    /// </summary>
    /// <remarks>
    /// **When to call:** On app startup, before any login or register screen.
    ///
    /// **Response (`data`):**
    /// - `teacher` — teacher app: which fields to show, OTP via Email or Sms, hint text.
    /// - `student` — student/parent app: same shape.
    /// - `otp.length`, `otp.expirySeconds` — OTP input and optional timer.
    ///
    /// **No** `Authorization` header. Settings come from DB key `Auth.Settings` (see `docs/Auth-Config-Frontend.md`).
    ///
    /// **Next APIs (OTP via email when `otpDelivery` is Email):**
    /// - Teacher: `POST …/Authentication/Teacher/LoginOrRegister` → `POST …/Teacher/VerifyOtp`
    /// - Student/parent: `POST …/Authentication/Student/SendOtp` → `POST …/Student/VerifyOtp`
    ///
    /// Email is sent by Messaging API (SMTP `mail.dmail.sa`) using bilingual HTML from auth settings.
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("Config")]
    [ProducesResponseType(typeof(AuthConfigResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthConfig()
    {
        return NewResult(await Mediator.Send(new GetAuthConfigQuery()));
    }
}
