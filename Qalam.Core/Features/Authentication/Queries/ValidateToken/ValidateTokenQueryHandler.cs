using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;
using System.Threading;
using System.Threading.Tasks;

namespace Qalam.Core.Features.Authentication.Queries.ValidateToken
{
    public class ValidateTokenQueryHandler : ResponseHandler, IRequestHandler<ValidateTokenQuery, Response<string>>
    {
        private readonly IAuthenticationService _authenticationService;

        public ValidateTokenQueryHandler(
            IStringLocalizer<SharedResources> sharedLocalizer,
            IAuthenticationService authenticationService) : base(sharedLocalizer)
        {
            _authenticationService = authenticationService;
        }

        public async Task<Response<string>> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
        {
            var result = await _authenticationService.ValidateToken(request.AccessToken);

            if (result == "NotExpired")
            {
                return Success<string>("Token is valid and not expired");
            }
            else if (result == "Expired")
            {
                return Unauthorized<string>("Token has expired");
            }
            else
            {
                return Unauthorized<string>("Invalid token");
            }
        }
    }
}

