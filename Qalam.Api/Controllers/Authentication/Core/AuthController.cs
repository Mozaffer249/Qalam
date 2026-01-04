using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Authentication.Commands.Register;
using Qalam.Core.Features.Authentication.Commands.Login;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Authentication.Core
{
    /// <summary>
    /// Core authentication operations: Register, Login, Logout, Token Management
    /// </summary>
    public class AuthController : AppControllerBase
    {
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
    }
}

