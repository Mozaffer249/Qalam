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

namespace Qalam.Core.Features.Authentication.Queries.GetSecurityEvents
{
    public class GetSecurityEventsQueryHandler : ResponseHandler, IRequestHandler<GetSecurityEventsQuery, Response<List<SecurityEvent>>>
    {
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

        public GetSecurityEventsQueryHandler(
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<AuthenticationResources> authLocalizer) : base(authLocalizer)
        {
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _authLocalizer = authLocalizer;
        }

        public async Task<Response<List<SecurityEvent>>> Handle(GetSecurityEventsQuery request, CancellationToken cancellationToken)
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized<List<SecurityEvent>>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            var events = await _auditService.GetUserSecurityEventsAsync(userId, request.PageNumber, request.PageSize);

            return Success<List<SecurityEvent>>(entity: events);
        }
    }
}

