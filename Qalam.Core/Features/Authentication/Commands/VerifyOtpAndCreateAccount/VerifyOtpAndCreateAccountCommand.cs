using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;

public class VerifyOtpAndCreateAccountCommand : IRequest<Response<string>>
{
    public string PhoneNumber { get; set; } = null!;
    public string OtpCode { get; set; } = null!;
}
