using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Authentication.Commands.UnregisterDeviceToken;

public class UnregisterDeviceTokenCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public string Token { get; set; } = null!;
}
