using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.SendPhoneOtp;

public class SendPhoneOtpCommand : IRequest<Response<string>>
{
    public string CountryCode { get; set; } = "+966";
    public string PhoneNumber { get; set; } = null!;
}
