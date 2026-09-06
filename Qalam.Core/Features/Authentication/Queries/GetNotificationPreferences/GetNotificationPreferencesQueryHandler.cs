using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Account;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Queries.GetNotificationPreferences;

public class GetNotificationPreferencesQueryHandler : ResponseHandler,
    IRequestHandler<GetNotificationPreferencesQuery, Response<NotificationPreferencesDto>>
{
    private readonly INotificationPreferenceService _preferences;

    public GetNotificationPreferencesQueryHandler(
        INotificationPreferenceService preferences,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _preferences = preferences;
    }

    public async Task<Response<NotificationPreferencesDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _preferences.GetAsync(request.UserId, cancellationToken);
        return Success(entity: dto);
    }
}
