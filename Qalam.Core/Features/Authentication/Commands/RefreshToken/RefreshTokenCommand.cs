using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<Response<JwtAuthResult>>
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public string? DeviceId { get; set; }
    }
}

