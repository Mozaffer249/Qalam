using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;

public class VerifyOtpAndCreateAccountCommand : IRequest<Response<object>>
{
    public string PhoneNumber { get; set; } = null!;
    public string OtpCode { get; set; } = null!;

    /// <summary>
    /// When true, records Terms &amp; Privacy acceptance on account create / login.
    /// Required for new users; optional for returning users who already accepted.
    /// </summary>
    public bool AcceptedTerms { get; set; }

    /// <summary>Optional FCM / push device token to register after successful verify.</summary>
    public string? DeviceToken { get; set; }

    /// <summary>ios | android | web — defaults to android when DeviceToken is set.</summary>
    public string? DeviceTokenPlatform { get; set; }

    public string? AppVersion { get; set; }
}
