using System.ComponentModel.DataAnnotations;
using MediatR;
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
    }
}

