using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Platform;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.UpdateOsrNotificationSettings;

public class UpdateOsrNotificationSettingsCommandHandler : ResponseHandler,
    IRequestHandler<UpdateOsrNotificationSettingsCommand, Response<OsrNotificationSettingsDto>>
{
    private readonly IOsrNotificationSettingsProvider _provider;

    public UpdateOsrNotificationSettingsCommandHandler(
        IOsrNotificationSettingsProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
    }

    public async Task<Response<OsrNotificationSettingsDto>> Handle(
        UpdateOsrNotificationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var saved = await _provider.SaveSettingsAsync(request.Settings, cancellationToken);
        return Success("OSR notification settings updated successfully", entity: saved);
    }
}
