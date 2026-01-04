using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Service.Abstracts;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Qalam.Core.Features.Authentication.Queries.GetActiveSessions
{
    public class GetActiveSessionsQueryHandler : ResponseHandler, IRequestHandler<GetActiveSessionsQuery, Response<List<SessionResponse>>>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;
        private readonly ISessionManagementService _sessionService;

        public GetActiveSessionsQueryHandler(
            IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<AuthenticationResources> authLocalizer,
            ISessionManagementService sessionService) : base(authLocalizer)
        {
            _httpContextAccessor = httpContextAccessor;
            _authLocalizer = authLocalizer;
            _sessionService = sessionService;
        }

        public async Task<Response<List<SessionResponse>>> Handle(GetActiveSessionsQuery request, CancellationToken cancellationToken)
        {
            // Get current user from HttpContext
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized<List<SessionResponse>>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            var currentIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "";

            var sessions = await _sessionService.GetActiveSessionsAsync(userId);

            var response = sessions.Select(s => new SessionResponse
            {
                SessionId = s.Id,
                DeviceInfo = s.DeviceName,
                IpAddress = s.IpAddress,
                LoginTime = s.LoginTime,
                LastActivity = s.LastActivityTime,
                IsCurrent = s.IpAddress == currentIp
            }).ToList();

            return Success<List<SessionResponse>>(entity: response);
        }
    }
}

