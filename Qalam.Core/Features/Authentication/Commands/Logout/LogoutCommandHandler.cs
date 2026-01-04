using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Service.Abstracts;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Qalam.Core.Features.Authentication.Commands.Logout
{
    public class LogoutCommandHandler : ResponseHandler, IRequestHandler<LogoutCommand, Response<string>>
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

        public LogoutCommandHandler(
            IAuthenticationService authenticationService,
            IStringLocalizer<AuthenticationResources> authLocalizer) : base(authLocalizer)
        {
            _authenticationService = authenticationService;
            _authLocalizer = authLocalizer;
        }

        public async Task<Response<string>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // Extract user ID from token
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(request.AccessToken);
            var userIdClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            // Revoke token(s)
            var result = await _authenticationService.RevokeTokenAsync(
                request.AccessToken,
                request.RefreshToken,
                userId,
                request.LogoutAllDevices
            );

            if (!result)
            {
                return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.LogoutFailed]);
            }

            return Success<string>(_authLocalizer[AuthenticationResourcesKeys.LoggedOutSuccessfully]);
        }
    }
}

