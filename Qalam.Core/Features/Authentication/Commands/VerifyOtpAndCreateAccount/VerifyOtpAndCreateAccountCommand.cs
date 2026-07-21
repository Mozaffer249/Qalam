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
}
