using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;

public class VerifyOtpAndCreateAccountCommand : IRequest<Response<object>>
{
    public string PhoneNumber { get; set; } = null!;
    public string OtpCode { get; set; } = null!;
}
