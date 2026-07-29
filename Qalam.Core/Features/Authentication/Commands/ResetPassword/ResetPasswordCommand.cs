using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordCommand : IRequest<Response<string>>
    {
        public string Email { get; set; } = default!;

        [StringLength(6, MinimumLength = 6, ErrorMessage = "Reset code must be exactly 6 digits")]
        public string ResetCode { get; set; } = default!;

        public string NewPassword { get; set; } = default!;
        public string ConfirmPassword { get; set; } = default!;

        /// <summary>
        /// When true, only Admin / SuperAdmin accounts may reset via this flow.
        /// Set by the Admin controller endpoint; not accepted from the client body.
        /// </summary>
        [BindNever]
        public bool RequireAdminRole { get; set; }
    }
}

