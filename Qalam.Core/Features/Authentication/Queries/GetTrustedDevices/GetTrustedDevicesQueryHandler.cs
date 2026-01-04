using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Identity;
using Qalam.Service.Abstracts;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Qalam.Core.Features.Authentication.Queries.GetTrustedDevices
{
    public class GetTrustedDevicesQueryHandler : ResponseHandler, IRequestHandler<GetTrustedDevicesQuery, Response<List<TrustedDevice>>>
    {
        private readonly ISessionManagementService _sessionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

        public GetTrustedDevicesQueryHandler(
            ISessionManagementService sessionService,
            IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<AuthenticationResources> authLocalizer) : base(authLocalizer)
        {
            _sessionService = sessionService;
            _httpContextAccessor = httpContextAccessor;
            _authLocalizer = authLocalizer;
        }

        public async Task<Response<List<TrustedDevice>>> Handle(GetTrustedDevicesQuery request, CancellationToken cancellationToken)
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized<List<TrustedDevice>>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            var devices = await _sessionService.GetTrustedDevicesAsync(userId);

            return Success<List<TrustedDevice>>(entity: devices);
        }
    }
}

