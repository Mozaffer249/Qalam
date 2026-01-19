using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Qalam.Core.Contracts;

namespace Qalam.Core.Behaviors
{
    public class UserIdentityBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserIdentityBehavior(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<TResponse> Handle(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            // Only process if request implements IAuthenticatedRequest
            if (request is IAuthenticatedRequest authenticatedRequest)
            {
                // Extract UserId from JWT claims
                var userIdClaim = _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? _httpContextAccessor.HttpContext?.User.FindFirst("uid")?.Value;

                if (int.TryParse(userIdClaim, out int userId))
                {
                    authenticatedRequest.UserId = userId;
                }
            }

            return await next();
        }
    }
}
