using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Core.Features.Authentication.Commands.Register;
using Qalam.Core.Features.Authentication.Commands.Login;
using Qalam.Core.Features.Authentication.Commands.SendPhoneOtp;
using Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;
using Qalam.Core.Features.Authentication.Commands.CompletePersonalInfo;
using Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Common;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Authentication.Core
{
    /// <summary>
    /// Core authentication operations: Register, Login, Logout, Token Management
    /// </summary>
    public class AuthController : AppControllerBase
    {
        private readonly IEnumService _enumService;

        public AuthController(IEnumService enumService)
        {
            _enumService = enumService;
        }
        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="command">Registration details including username, email, password</param>
        /// <returns>Registration result with user information</returns>
        [HttpPost(Router.AuthenticationRegister)]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Login with username and password
        /// </summary>
        /// <param name="command">Login credentials (username/email and password)</param>
        /// <returns>JWT access token and refresh token</returns>
        [HttpPost(Router.AuthenticationLogin)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        #region Teacher Registration - 4 Steps

        /// <summary>
        /// Step 1: Send OTP to phone number
        /// </summary>
        /// <param name="command">Country code and phone number</param>
        /// <returns>Success message if OTP sent</returns>
        [HttpPost(Router.TeacherStep1SendOtp)]
        public async Task<IActionResult> SendPhoneOtp([FromBody] SendPhoneOtpCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Step 2: Verify OTP and create basic account
        /// </summary>
        /// <param name="command">Phone number and OTP code</param>
        /// <returns>User ID and temporary JWT token</returns>
        [HttpPost(Router.TeacherStep2VerifyOtp)]
        public async Task<IActionResult> VerifyOtpAndCreateAccount([FromBody] VerifyOtpAndCreateAccountCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Step 3: Complete personal information
        /// </summary>
        /// <param name="command">Name, email, and password</param>
        /// <returns>Teacher ID and full JWT token</returns>
        [HttpPost(Router.TeacherStep3PersonalInfo)]
        [Authorize] // Requires token from step 2
        public async Task<IActionResult> CompletePersonalInfo([FromBody] CompletePersonalInfoCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Step 4: Upload identity documents and certificates
        /// </summary>
        /// <param name="command">Identity document, certificates, and location info</param>
        /// <returns>Success message if documents uploaded</returns>
        [HttpPost(Router.TeacherStep4UploadDocuments)]
        [Authorize(Roles = Roles.Teacher)] // Requires completion of step 3
        public async Task<IActionResult> UploadTeacherDocuments([FromForm] UploadTeacherDocumentsCommand command)
        {
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

