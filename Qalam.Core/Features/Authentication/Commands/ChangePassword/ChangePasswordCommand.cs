using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordCommand : IRequest<Response<string>>
    {
        public string CurrentPassword { get; set; } = default!;
        public string NewPassword { get; set; } = default!;
        public string ConfirmPassword { get; set; } = default!;
    }
}

