using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.SendResetPasswordCode
{
    public class SendResetPasswordCodeCommand : IRequest<Response<string>>
    {
        public string Email { get; set; } = default!;

        /// <summary>
        /// When true, only Admin / SuperAdmin accounts may receive a reset code.
        /// Set by the Admin controller endpoint; not accepted from the client body.
        /// </summary>
        [BindNever]
        public bool RequireAdminRole { get; set; }
    }
}

