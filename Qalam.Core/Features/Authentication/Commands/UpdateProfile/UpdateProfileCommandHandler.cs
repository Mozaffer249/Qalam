using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Identity;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Qalam.Core.Features.Authentication.Commands.UpdateProfile
{
    public class UpdateProfileCommandHandler : ResponseHandler, IRequestHandler<UpdateProfileCommand, Response<string>>
    {
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

        public UpdateProfileCommandHandler(
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<AuthenticationResources> authLocalizer) : base(authLocalizer)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _authLocalizer = authLocalizer;
        }

        public async Task<Response<string>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            // Get current user from HttpContext
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized<string>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            var user = await _userManager.FindByIdAsync(userIdClaim);
            if (user == null)
            {
                return NotFound<string>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;
            
            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;
            
            if (request.Address != null)
                user.Address = request.Address;
            
            if (request.Nationality != null)
                user.Nationality = request.Nationality;
            
            if (request.PhoneNumber != null)
                user.PhoneNumber = request.PhoneNumber;
            
            if (request.ProfilePictureUrl != null)
                user.ProfilePictureUrl = request.ProfilePictureUrl;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest<string>(errors);
            }

            return Success<string>(_authLocalizer[AuthenticationResourcesKeys.ProfileUpdatedSuccessfully]);
        }
    }
}

