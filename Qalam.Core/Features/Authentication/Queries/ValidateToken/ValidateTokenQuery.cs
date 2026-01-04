using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Queries.ValidateToken
{
    public class ValidateTokenQuery : IRequest<Response<string>>
    {
        public string AccessToken { get; set; } = default!;
    }
}

