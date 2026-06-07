using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Core.Features.Authentication.Commands.Register;
using Qalam.Core.Features.Authentication.Commands.Login;
using Qalam.Core.Features.Authentication.Commands.SendPhoneOtp;
using Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;
using Qalam.Core.Features.Authentication.Commands.CompletePersonalInfo;
using Qalam.Core.Features.Authentication.Queries.GetTeacherRegistrationRequirements;
using Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;
using Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;
using Qalam.Api.Helpers;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Common;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Authentication.Core
{
    /// <summary>
    /// Core authentication operations: admin login, teacher registration flow, and enum helpers.
    /// </summary>
    public class AuthController : AppControllerBase
    {
        private readonly IEnumService _enumService;

        public AuthController(IEnumService enumService)
        {
            _enumService = enumService;
        }

        // /// <summary>
        // /// Register a new user account
        // /// </summary>
        // /// <param name="command">Registration details including username, email, password</param>
        // /// <returns>Registration result with user information</returns>
        // [HttpPost(Router.AuthenticationRegister)]
        // public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        // {
        //     return NewResult(await Mediator.Send(command));
        // }

        /// <summary>
        /// Login with username and password
        /// </summary>
        /// <param name="command">Login credentials (username/email and password)</param>
        /// <returns>JWT access token and refresh token</returns>
        [HttpPost(Router.AdminLogin)]
        public async Task<IActionResult> AdminLogin([FromBody] LoginCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        #region Teacher Authentication & Registration

        /// <summary>
        /// Teacher login or register — send OTP (phone + email when configured).
        /// </summary>
        /// <remarks>
        /// **Prerequisites:** `GET /Api/V1/Authentication/Config` → use `data.teacher` (`otpDelivery`, `emailRequired`, hints).
        ///
        /// When `teacher.otpDelivery` is **Email**, the OTP is sent via SMTP (Messaging API → `info@qalam.net.sa`).
        /// Response: `otpSentTo`, `maskedDestination`, `isNewUser`. Then call `POST …/Teacher/VerifyOtp`.
        ///
        /// Body must include `email` when `teacher.registerRequiresEmail` is true (new users).
        /// </remarks>
        [Tags("Teacher Authentication")]
        [HttpPost(Router.TeacherLoginOrRegister)]
        public async Task<IActionResult> TeacherLoginOrRegister([FromBody] SendPhoneOtpCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Teacher — verify OTP from email or SMS (step 2).
        /// </summary>
        /// <remarks>
        /// Verifies the code sent by `LoginOrRegister`. When delivery was email, user enters the code from the bilingual HTML email.
        /// </remarks>
        [Tags("Teacher Authentication")]
        [HttpPost(Router.TeacherVerifyOtp)]
        public async Task<IActionResult> TeacherVerifyOtp([FromBody] VerifyOtpAndCreateAccountCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Complete personal information (teacher registration step 3).
        /// </summary>
        /// <param name="command">First name, last name, and password. Email is optional if already collected at OTP (steps 1–2).</param>
        /// <returns>Teacher ID and full JWT token</returns>
        /// <remarks>
        /// Requires JWT from `POST …/Teacher/VerifyOtp`. Body: `firstName`, `lastName`, `password` only.
        /// Omit `email` when it was already set during `LoginOrRegister` / `VerifyOtp` (typical email-OTP flow).
        /// Next step: `GET …/Teacher/RegistrationRequirements`, then `POST …/Teacher/SubmitRegistrationRequirements`.
        /// </remarks>
        [Tags("Teacher Authentication")]
        [HttpPost(Router.TeacherCompletePersonalInfo)]
        [Authorize] // Requires token from VerifyOtp
        public async Task<IActionResult> CompletePersonalInfo([FromBody] CompletePersonalInfoCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Get active teacher registration requirements (wizard step 4 — before submit).
        /// </summary>
        /// <returns>Active requirements only: code, type, labels, validation limits</returns>
        /// <remarks>
        /// **No auth required.** Call after `CompletePersonalInfo` to build the documents/bio/location step.
        ///
        /// Response `data.requirements[]` items include `code`, `requirementType`, `isRequired`, `minCount`, `maxCount`,
        /// `allowedExtensions`, `maxFileSizeBytes`, `maxLength` (text).
        ///
        /// Seeded codes: `identity_document`, `certificate`, `bio`, `location`. Admins may add custom file requirements.
        ///
        /// See `docs/Teacher-Registration-Guide.md`.
        /// </remarks>
        [AllowAnonymous]
        [Tags("Teacher Authentication")]
        [HttpGet(Router.TeacherRegistrationRequirements)]
        [ProducesResponseType(typeof(Qalam.Data.DTOs.Teacher.TeacherRegistrationRequirementsResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTeacherRegistrationRequirements()
        {
            return NewResult(await Mediator.Send(new GetTeacherRegistrationRequirementsQuery()));
        }

        /// <summary>
        /// Submit teacher registration requirements (wizard step 4 — multipart).
        /// </summary>
        /// <param name="command">Form fields validated against active requirements from the catalog</param>
        /// <returns>Success when all required items are submitted; teacher moves to pending verification</returns>
        /// <remarks>
        /// Requires **Teacher** JWT. Content-Type: `multipart/form-data`.
        ///
        /// **Standard fields** (when corresponding requirement is active):
        /// - `isInSaudiArabia` — boolean requirement `location`
        /// - `bio` — text requirement `bio`
        /// - `identityType`, `documentNumber`, `issuingCountryCode`, `identityDocumentFile` — `identity_document`
        /// - `certificates[i].file`, title, issuer, dates — `certificate` (min/max count enforced)
        ///
        /// **Custom file requirements:** form field `file_{code}` (e.g. `file_custom_cv`).
        ///
        /// Only **active + required** items are validated; optional items may be omitted.
        ///
        /// See `docs/Teacher-Registration-Guide.md`.
        /// </remarks>
        [Tags("Teacher Authentication")]
        [HttpPost(Router.TeacherSubmitRegistrationRequirements)]
        [Authorize(Roles = Roles.Teacher)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitTeacherRegistrationRequirements(
            [FromForm] SubmitTeacherRegistrationRequirementsCommand command)
        {
            command.CustomFilesByCode = TeacherRegistrationFormHelper.ParseCustomFilesByCode(Request);
            command.TextValuesByCode = TeacherRegistrationFormHelper.ParseTextValuesByCode(Request);
            command.BoolValuesByCode = TeacherRegistrationFormHelper.ParseBoolValuesByCode(Request);
            command.SelectionsByCode = TeacherRegistrationFormHelper.ParseSelectionsByCode(Request);

            // Normalize the system-coded fixed fields into the generic dicts so the validator
            // and service can dispatch off `code` uniformly without special-casing bio/location.
            command.TextValuesByCode.TryAdd(TeacherRegistrationRequirementCodes.Bio, command.Bio);
            command.BoolValuesByCode.TryAdd(TeacherRegistrationRequirementCodes.Location, command.IsInSaudiArabia);

            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Upload identity documents and certificates (obsolete).
        /// </summary>
        /// <param name="command">Legacy multipart payload — same shape as SubmitRegistrationRequirements</param>
        /// <returns>Success message if documents uploaded</returns>
        /// <remarks>
        /// **Deprecated.** Use `POST …/Teacher/SubmitRegistrationRequirements` instead.
        /// Delegates to the same handler for backward-compatible mobile builds.
        /// </remarks>
        [Obsolete("Use POST …/Teacher/SubmitRegistrationRequirements")]
        [HttpPost(Router.TeacherUploadDocuments)]
        [Authorize(Roles = Roles.Teacher)]
        [Tags("Teacher Authentication")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadTeacherDocuments([FromForm] UploadTeacherDocumentsCommand command)
        {
            command.CustomFilesByCode = TeacherRegistrationFormHelper.ParseCustomFilesByCode(Request);
            command.TextValuesByCode = TeacherRegistrationFormHelper.ParseTextValuesByCode(Request);
            command.BoolValuesByCode = TeacherRegistrationFormHelper.ParseBoolValuesByCode(Request);
            command.SelectionsByCode = TeacherRegistrationFormHelper.ParseSelectionsByCode(Request);
            return NewResult(await Mediator.Send(command));
        }

        #endregion

        #region Enum Helpers

        /// <summary>
        /// Get available identity types (optionally filtered by location)
        /// </summary>
        /// <param name="isInSaudiArabia">Filter by location: true for Saudi Arabia, false for outside, null for all</param>
        /// <returns>List of identity types with translations</returns>
        [HttpGet(Router.GetIdentityTypes)]
        public IActionResult GetIdentityTypes([FromQuery] bool? isInSaudiArabia = null)
        {
            var identityTypes = _enumService.GetIdentityTypes(isInSaudiArabia);
            return Ok(new Response<List<EnumItemDto>>
            {
                Data = identityTypes,
                Succeeded = true
            });
        }

        /// <summary>
        /// Get all teacher document types
        /// </summary>
        /// <returns>List of document types with translations</returns>
        [HttpGet(Router.GetDocumentTypes)]
        public IActionResult GetDocumentTypes()
        {
            var documentTypes = _enumService.GetTeacherDocumentTypes();
            return Ok(new Response<List<EnumItemDto>>
            {
                Data = documentTypes,
                Succeeded = true
            });
        }
        #endregion
    }
}

