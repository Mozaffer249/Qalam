using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.SendResetPasswordCode
{
    public class SendResetPasswordCodeCommand : IRequest<Response<string>>
    {
        public string Email { get; set; } = default!;
    }
}

