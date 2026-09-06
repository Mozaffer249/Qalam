using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.DTOs.Account;
using Qalam.Service.Abstracts;
using System.Net;

namespace Qalam.Core.Features.Authentication.Commands.DeactivateAccount;

public class DeactivateAccountCommandHandler : ResponseHandler,
    IRequestHandler<DeactivateAccountCommand, Response<string>>
{
    private readonly IAccountDeactivationService _deactivationService;
    private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

    public DeactivateAccountCommandHandler(
        IAccountDeactivationService deactivationService,
        IStringLocalizer<AuthenticationResources> authLocalizer) : base(authLocalizer)
    {
        _deactivationService = deactivationService;
        _authLocalizer = authLocalizer;
    }

    public async Task<Response<string>> Handle(
        DeactivateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var (success, message, blockingReasons) = await _deactivationService.DeactivateAsync(
            request.UserId,
            request.Password,
            request.AccessToken,
            request.RefreshToken,
            request.IpAddress,
            cancellationToken);

        if (success)
        {
            return Success<string>(
                _authLocalizer[AuthenticationResourcesKeys.AccountDeletedSuccessfully]
                ?? message);
        }

        if (blockingReasons is { Count: > 0 })
        {
            return new Response<string>
            {
                Succeeded = false,
                StatusCode = HttpStatusCode.BadRequest,
                Message = message,
                Data = null!,
                Meta = new AccountDeactivationBlockedDto
                {
                    BlockingReasons = blockingReasons.ToList()
                }
            };
        }

        if (string.Equals(message, "Password is incorrect", StringComparison.OrdinalIgnoreCase))
            return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.PasswordNotCorrect]);

        if (string.Equals(message, "User not found", StringComparison.OrdinalIgnoreCase))
            return NotFound<string>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);

        if (message.Contains("Only student or guardian", StringComparison.OrdinalIgnoreCase))
            return Forbidden<string>(message);

        return BadRequest<string>(message);
    }
}
