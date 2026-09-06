using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Authentication.Commands.DeactivateAccount;

public class DeactivateAccountCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public string Password { get; set; } = null!;

    [BindNever]
    public string? AccessToken { get; set; }

    [BindNever]
    public string? RefreshToken { get; set; }

    [BindNever]
    public string? IpAddress { get; set; }
}
