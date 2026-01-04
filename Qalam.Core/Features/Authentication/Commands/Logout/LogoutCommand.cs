using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.Logout
{
    public class LogoutCommand : IRequest<Response<string>>
    {
        public string AccessToken { get; set; } = default!;
        public string? RefreshToken { get; set; }
        public bool LogoutAllDevices { get; set; } = false;
    }
}

