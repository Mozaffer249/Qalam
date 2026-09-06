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
    public string PhoneNumber { get; set; } = default!;
    public string OtpCode { get; set; } = default!;

    /// <summary>Optional FCM / push device token to register after successful verify.</summary>
    public string? DeviceToken { get; set; }

    /// <summary>ios | android | web — defaults to android when DeviceToken is set.</summary>
    public string? DeviceTokenPlatform { get; set; }

    public string? AppVersion { get; set; }
}
