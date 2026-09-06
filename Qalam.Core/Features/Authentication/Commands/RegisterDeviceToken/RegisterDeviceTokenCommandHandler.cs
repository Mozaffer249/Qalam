using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.RegisterDeviceToken;

public class RegisterDeviceTokenCommandHandler : ResponseHandler,
    IRequestHandler<RegisterDeviceTokenCommand, Response<string>>
{
    private readonly IUserDeviceTokenRepository _deviceTokens;

    public RegisterDeviceTokenCommandHandler(
        IUserDeviceTokenRepository deviceTokens,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _deviceTokens = deviceTokens;
    }

    public async Task<Response<string>> Handle(
        RegisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        await _deviceTokens.UpsertAsync(
            request.UserId,
            request.Token.Trim(),
            request.Platform.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(request.AppVersion) ? null : request.AppVersion.Trim(),
            cancellationToken);

        return Success<string>("Device token registered");
    }
}
