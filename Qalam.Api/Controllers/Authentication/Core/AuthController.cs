using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Core.Features.Authentication.Commands.AcceptTeacherTerms;
using Qalam.Core.Features.Authentication.Commands.Register;
using Qalam.Core.Features.Authentication.Commands.Login;
using Qalam.Core.Features.Authentication.Commands.SendPhoneOtp;
using Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;
using Qalam.Core.Features.Authentication.Commands.CompletePersonalInfo;
using Qalam.Core.Features.Authentication.Commands.UpdateProfile;
using Qalam.Core.Features.Authentication.Queries.GetProfile;
using Qalam.Core.Features.Authentication.Queries.GetTeacherRegistrationRequirements;
using Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;
using Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;
using Qalam.Api.Helpers;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Common;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using System.Net;

namespace Qalam.Api.Controllers.Authentication.Core
{
    /// <summary>
    /// Core authentication operations: admin login, teacher registration flow, and enum helpers.
    /// </summary>
    public class AuthController : AppControllerBase
    {
        private readonly IEnumService _enumService;
        private readonly ITeacherRegistrationStatusService _registrationStatusService;
        private readonly ITeacherRepository _teacherRepository;

        public AuthController(
            IEnumService enumService,
            ITeacherRegistrationStatusService registrationStatusService,
            ITeacherRepository teacherRepository)
        {
            _enumService = enumService;
            _registrationStatusService = registrationStatusService;
            _teacherRepository = teacherRepository;
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
        /// - `location` — residence: InsideSaudiArabia | OutsideSaudiArabia (drives identity types + Teacher.Location)
        /// - `nationalityCode` — ISO2 nationality (profile data only)
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
            // and service can dispatch off `code` uniformly without special-casing bio.
            command.TextValuesByCode.TryAdd(TeacherRegistrationRequirementCodes.Bio, command.Bio);

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

        /// <summary>
        /// Re-check teacher account activation status and registration routing.
        /// </summary>
        /// <returns>Account flags and `nextStep` for UI routing</returns>
        /// <remarks>
        /// Requires **Teacher** JWT. Lightweight poll endpoint for the waiting screen — does not return
        /// the full document checklist. Use `GET …/Teacher/TeacherDocuments/Status` when rejection reasons
        /// or re-upload document IDs are needed.
        ///
        /// Poll every few seconds on the waiting page:
        /// - `awaitingFinalApproval === true` → show **Awaiting final approval** section
        /// - `isAccountActivated` and `requiresAvailabilitySetup` → navigate to availability setup
        /// - `isAccountActivated` and `!requiresAvailabilitySetup` → navigate to dashboard (`nextStep.nextStepName`)
        ///
        /// Blocked teachers receive 403 from global middleware.
        /// </remarks>
        [Tags("Teacher Authentication")]
        [HttpGet(Router.TeacherAccountStatus)]
        [Authorize(Roles = Roles.Teacher)]
        [ProducesResponseType(typeof(TeacherAccountStatusResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTeacherAccountStatus()
        {
            var userId = GetUserId();
            var teacher = await _teacherRepository.GetByUserIdAsync(userId);

            if (teacher == null)
                return NewResult(new Response<TeacherAccountStatusResponseDto?>("Teacher profile not found")
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            var status = await _registrationStatusService.GetAccountStatusForTeacherAsync(teacher.Id, userId);
            return NewResult(new Response<TeacherAccountStatusResponseDto>(status)
            {
                StatusCode = HttpStatusCode.OK
            });
        }

        /// <summary>
        /// Record that the authenticated teacher accepted Terms &amp; Privacy.
        /// Idempotent — safe to call if already accepted.
        /// </summary>
        [Tags("Teacher Authentication")]
        [HttpPost(Router.TeacherAcceptTerms)]
        [Authorize(Roles = Roles.Teacher)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> AcceptTeacherTerms()
        {
            return NewResult(await Mediator.Send(new AcceptTeacherTermsCommand()));
        }

        #endregion

        #region Account profile

        [Authorize]
        [HttpGet(Router.AccountGetProfile)]
        [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProfile()
            => NewResult(await Mediator.Send(new GetProfileQuery()));

        [Authorize]
        [HttpPost(Router.AccountUpdateProfile)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
            => NewResult(await Mediator.Send(command));

        #endregion

        #region Enum Helpers

        /// <summary>
        /// Get available identity types filtered by residence location (بلد الإقامة).
        /// </summary>
        /// <param name="location">
        /// InsideSaudiArabia → National ID / Iqama; OutsideSaudiArabia → Passport / License / Government ID;
        /// omit for all types.
        /// </param>
        /// <param name="nationalityCode">Deprecated; ignored. Kept for older clients.</param>
        /// <returns>List of identity types with translations</returns>
        [HttpGet(Router.GetIdentityTypes)]
        public IActionResult GetIdentityTypes(
            [FromQuery] TeacherLocation? location = null,
            [FromQuery] string? nationalityCode = null)
        {
            var identityTypes = _enumService.GetIdentityTypes(location, nationalityCode);
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

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }
    }
}

