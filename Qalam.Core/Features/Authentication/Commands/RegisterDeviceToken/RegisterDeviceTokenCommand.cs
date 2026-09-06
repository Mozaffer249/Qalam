using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Authentication.Commands.RegisterDeviceToken;

public class RegisterDeviceTokenCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public string Token { get; set; } = null!;
    public string Platform { get; set; } = null!;
    public string? AppVersion { get; set; }
}
