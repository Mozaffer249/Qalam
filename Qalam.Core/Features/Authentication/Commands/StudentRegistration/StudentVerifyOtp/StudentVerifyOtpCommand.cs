using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

/// <summary>
/// Verify OTP only (Screen 2). Returns token and NextStep (ChooseAccountType or Dashboard).
/// Account creation happens in SetAccountTypeAndUsage.
/// </summary>
public class StudentVerifyOtpCommand : IRequest<Response<StudentRegistrationResponseDto>>
{
    public string CountryCode { get; set; } = "+966";
    public string PhoneNumber { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}
