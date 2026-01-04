using System.ComponentModel.DataAnnotations;
using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.ConfirmEmail
{
    public class ConfirmEmailCommand : IRequest<Response<string>>
    {
        public int UserId { get; set; }

        [StringLength(4, MinimumLength = 4, ErrorMessage = "OTP code must be exactly 4 digits")]
        public string Code { get; set; } = default!;
    }
}

