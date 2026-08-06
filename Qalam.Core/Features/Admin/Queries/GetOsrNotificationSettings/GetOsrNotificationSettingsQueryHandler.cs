using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Platform;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetOsrNotificationSettings;

public class GetOsrNotificationSettingsQueryHandler : ResponseHandler,
    IRequestHandler<GetOsrNotificationSettingsQuery, Response<OsrNotificationSettingsDto>>
{
    private readonly IOsrNotificationSettingsProvider _provider;

    public GetOsrNotificationSettingsQueryHandler(
        IOsrNotificationSettingsProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
    }

    public async Task<Response<OsrNotificationSettingsDto>> Handle(
        GetOsrNotificationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _provider.GetSettingsAsync(cancellationToken);
        return Success(entity: settings);
    }
}
