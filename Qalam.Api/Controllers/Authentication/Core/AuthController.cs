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

        #region Teacher Authentication & Registration

        /// <summary>
        /// Teacher Login or Register - Send OTP to phone number
        /// Works for both new and existing users
        /// </summary>
        /// <param name="command">Country code and phone number</param>
        /// <returns>IsNewUser flag and success message</returns>
        [HttpPost(Router.TeacherLoginOrRegister)]
        public async Task<IActionResult> TeacherLoginOrRegister([FromBody] SendPhoneOtpCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Verify OTP - Login for existing users, Register for new users
        /// </summary>
        /// <param name="command">Phone number and OTP code</param>
        /// <returns>JWT token, IsNewUser flag, and next registration step</returns>
        [HttpPost(Router.TeacherVerifyOtp)]
        public async Task<IActionResult> TeacherVerifyOtp([FromBody] VerifyOtpAndCreateAccountCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Complete personal information (for new users after registration)
        /// </summary>
        /// <param name="command">Name, email, and password</param>
        /// <returns>Teacher ID and full JWT token</returns>
        [HttpPost(Router.TeacherCompletePersonalInfo)]
        [Authorize] // Requires token from VerifyOtp
        public async Task<IActionResult> CompletePersonalInfo([FromBody] CompletePersonalInfoCommand command)
        {
            return NewResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Upload identity documents and certificates
        /// </summary>
        /// <param name="command">Identity document, certificates, and location info</param>
        /// <returns>Success message if documents uploaded</returns>
        [HttpPost(Router.TeacherUploadDocuments)]
        [Authorize(Roles = Roles.Teacher)] // Requires Teacher role
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

