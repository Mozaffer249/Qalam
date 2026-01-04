using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Identity;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Qalam.Core.Features.Authentication.Queries.GetProfile
{
    public class GetProfileQueryHandler : ResponseHandler, IRequestHandler<GetProfileQuery, Response<ProfileResponse>>
    {
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

        public GetProfileQueryHandler(
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<AuthenticationResources> authLocalizer) : base(authLocalizer)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _authLocalizer = authLocalizer;
        }

        public async Task<Response<ProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        {
            // Get current user from HttpContext
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized<ProfileResponse>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            var user = await _userManager.FindByIdAsync(userIdClaim);
            if (user == null)
            {
                return NotFound<ProfileResponse>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            var response = new ProfileResponse
            {
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                Nationality = user.Nationality,
                ProfilePictureUrl = user.ProfilePictureUrl,
                PhoneNumber = user.PhoneNumber,
                TwoFactorEnabled = user.TwoFactorEnabled,
                EmailConfirmed = user.EmailConfirmed
            };

            return Success<ProfileResponse>(entity: response);
        }
    }
}

