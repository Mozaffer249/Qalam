using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Authentication.Commands.SendPhoneOtp;

public class SendPhoneOtpCommand : IRequest<Response<SendOtpResponseDto>>
{
    public string CountryCode { get; set; } = "+966";
    public string PhoneNumber { get; set; } = null!;
}
