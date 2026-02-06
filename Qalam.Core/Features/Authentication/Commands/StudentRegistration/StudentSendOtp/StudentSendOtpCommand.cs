using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

/// <summary>
/// Send OTP for student/parent registration (Screen 1 - phone only).
/// </summary>
public class StudentSendOtpCommand : IRequest<Response<StudentSendOtpResponseDto>>
{
    public string CountryCode { get; set; } = "+966";
    public string PhoneNumber { get; set; } = default!;
}
