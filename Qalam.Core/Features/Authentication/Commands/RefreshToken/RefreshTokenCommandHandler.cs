using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Qalam.Core.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : ResponseHandler, IRequestHandler<RefreshTokenCommand, Response<JwtAuthResult>>
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<SharedResources> _sharedLocalizer;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;
        private readonly ISessionManagementService _sessionService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RefreshTokenCommandHandler(
            IStringLocalizer<SharedResources> sharedLocalizer,
            IStringLocalizer<AuthenticationResources> authLocalizer,
            IAuthenticationService authenticationService,
            UserManager<User> userManager,
            ISessionManagementService sessionService,
            IHttpContextAccessor httpContextAccessor) : base(sharedLocalizer)
        {
            _authenticationService = authenticationService;
            _userManager = userManager;
            _sharedLocalizer = sharedLocalizer;
            _authLocalizer = authLocalizer;
            _sessionService = sessionService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response<JwtAuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // Validate refresh token
            var jwtToken = await _authenticationService.ReadJWTToken(request.AccessToken);

            if (jwtToken == null)
            {
                return BadRequest<JwtAuthResult>(_sharedLocalizer[SharedResourcesKeys.UnAuthorized]);
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "uid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return BadRequest<JwtAuthResult>(_sharedLocalizer[SharedResourcesKeys.UnAuthorized]);
            }

            var user = await _userManager.FindByIdAsync(userIdClaim);

            if (user == null)
            {
                return NotFound<JwtAuthResult>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            if (!user.IsActive)
            {
                return Unauthorized<JwtAuthResult>(_authLocalizer[AuthenticationResourcesKeys.UserIsNotActive]);
            }

            // Validate and refresh token
            var result = await _authenticationService.GetRefreshToken(user, jwtToken, request.RefreshToken);

            if (result == null)
            {
                return Unauthorized<JwtAuthResult>(_sharedLocalizer[SharedResourcesKeys.UnAuthorized]);
            }

            // Update session activity
            if (!string.IsNullOrEmpty(request.AccessToken))
            {
                await _sessionService.UpdateSessionActivityAsync(request.AccessToken);
            }

            // Create new session with new tokens
            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = GetClientIpAddress(httpContext);
                var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
                var deviceName = httpContext?.Request.Headers["Device-Name"].ToString() ?? "Unknown Device";

                await _sessionService.CreateSessionAsync(
                    userId: user.Id,
                    deviceId: request.DeviceId,
                    deviceName: deviceName,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    accessToken: result.AccessToken,
                    refreshToken: result.RefreshToken.TokenString
                );
            }

            return Success(entity: result);
        }

        private string GetClientIpAddress(HttpContext? context)
        {
            if (context == null) return "Unknown";

            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}

