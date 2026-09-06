using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.UnregisterDeviceToken;

public class UnregisterDeviceTokenCommandHandler : ResponseHandler,
    IRequestHandler<UnregisterDeviceTokenCommand, Response<string>>
{
    private readonly IUserDeviceTokenRepository _deviceTokens;

    public UnregisterDeviceTokenCommandHandler(
        IUserDeviceTokenRepository deviceTokens,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _deviceTokens = deviceTokens;
    }

    public async Task<Response<string>> Handle(
        UnregisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        var ok = await _deviceTokens.DeactivateAsync(
            request.UserId,
            request.Token.Trim(),
            cancellationToken);

        if (!ok)
            return NotFound<string>("Device token not found");

        return Success<string>("Device token unregistered");
    }
}
